using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.SmartNodeInvoker;

public class SmartNodeInvoker(IDatabase cache) : ISmartNodeInvoker
{
    private const string ScoreboardKey = "nodes:scoreboard";
    private readonly NodeScoringOptions _options = new ();

    public async Task<NodeResult<T>> GetDataFromNodesAsync<T>(
        IEnumerable<string> nodes,
        Func<string, Task<T>> dataRetrievalTask)
    {
        var stopwatch = Stopwatch.StartNew();
        var failed = new Dictionary<string, Exception>();
        var orderedNodes = await OrderHealthyNodesByScoreAsync(nodes);

        foreach (var node in orderedNodes)
        {
            try
            {
                var data = await dataRetrievalTask(node);
                await IncrementScoreAsync(node, _options.SuccessReward);

                stopwatch.Stop();
                return new NodeResult<T>
                {
                    Data = data,
                    SuccessfulNode = node,
                    FailedNodes = failed,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                failed[node] = ex;
                await IncrementScoreAsync(node, -GetPenalty(ex));
            }
        }

        throw new AggregateException("All nodes failed", failed.Values);
    }

    public async Task<NodeResult<T>> GetDataFromNodesParallelAsync<T>(
        IEnumerable<string> nodes,
        Func<string, Task<T>> dataRetrievalTask)
    {
        var stopwatch = Stopwatch.StartNew();
        var failed = new ConcurrentDictionary<string, Exception>();
        var orderedNodes = await OrderHealthyNodesByScoreAsync(nodes);

        var tasks = orderedNodes.Select(async node =>
        {
            try
            {
                var result = await dataRetrievalTask(node);
                await IncrementScoreAsync(node, _options.SuccessReward);

                return new NodeResult<T> { Data = result, SuccessfulNode = node };
            }
            catch (Exception ex)
            {
                await IncrementScoreAsync(node, -GetPenalty(ex));
                failed[node] = ex;
                return null;
            }
        }).ToList();

        var completed = await Task.WhenAny(tasks);
        var result = await completed;
        stopwatch.Stop();

        if (result != null)
        {
            result.ExecutionTime = stopwatch.Elapsed;
            result.FailedNodes = failed.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return result;
        }

        throw new AggregateException("All nodes failed", failed.Values);
    }

    private async Task<List<string>> OrderHealthyNodesByScoreAsync(IEnumerable<string> nodes)
    {
        var scored = await Task.WhenAll(nodes.Select(async node =>
        {
            var score = await GetScoreAsync(node);
            return new { Node = node, Score = score };
        }));

        return scored
            .Where(s => s.Score >= _options.CircuitBreakerThreshold)
            .OrderByDescending(s => s.Score)
            .Select(s => s.Node)
            .ToList();
    }

    private async Task<int> GetScoreAsync(string node)
    {
        var score = await cache.SortedSetScoreAsync(ScoreboardKey, node);
        return score.HasValue ? (int)score.Value : _options.MaxScore / 2;
    }

    private async Task IncrementScoreAsync(string node, int delta)
    {
        var newScore = await cache.SortedSetIncrementAsync(ScoreboardKey, node, delta);
        var clamped = Math.Clamp((int)newScore, _options.MinScore, _options.MaxScore);
        await cache.SortedSetAddAsync(ScoreboardKey, node, clamped);
    }

    private int GetPenalty(Exception ex)
    {
        return ex switch
        {
            TimeoutException => _options.FailurePenalties["Timeout"],
            HttpRequestException httpEx when httpEx.Message.Contains("401") => _options.FailurePenalties["Http401"],
            HttpRequestException httpEx when httpEx.Message.Contains("429") => _options.FailurePenalties["Http429"],
            HttpRequestException httpEx when httpEx.Message.Contains("5") => _options.FailurePenalties["Http5xx"],
            _ => _options.FailurePenalties["Other"]
        };
    }
}