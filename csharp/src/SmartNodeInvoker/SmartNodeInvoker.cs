using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Helpers;

namespace Train.Solver.SmartNodeInvoker;

public class SmartNodeInvoker(IDatabase cache) : ISmartNodeInvoker
{
    private const int MaxScore = 100;
    private const int MinScore = 0;
    private const int SuccessReward = 5;
    private const int FailurePenalty = 5;

    public async Task<NodeResult<T>> ExecuteAsync<T>(
        string networkName,
        IEnumerable<string> nodes,
        Func<string, Task<T>> dataRetrievalTask)
    {
        var stopwatch = Stopwatch.StartNew();
        var redisKey = RedisHelper.BuildNodeScoreKey(networkName);

        var failed = new Dictionary<string, Exception>();
        var orderedNodes = await OrderNodesByScoreAsync(redisKey, nodes);

        foreach (var node in orderedNodes)
        {
            try
            {
                var data = await dataRetrievalTask(node);
                await IncrementScoreAsync(redisKey, node, SuccessReward);

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
                await IncrementScoreAsync(redisKey, node, -FailurePenalty);
            }
        }

        return new NodeResult<T>
        {
            Data = default,
            SuccessfulNode = null,
            FailedNodes = failed,
            ExecutionTime = stopwatch.Elapsed
        };
    }

    private async Task<List<string>> OrderNodesByScoreAsync(string redisKey, IEnumerable<string> nodes)
    {
        var allScores = await cache.SortedSetRangeByScoreWithScoresAsync(redisKey);

        var scoreDict = allScores.ToDictionary(
            entry => (string)entry.Element,
            entry => (int)entry.Score
        );

        return nodes
            .Select(n => new { Node = n, Score = scoreDict.GetValueOrDefault(n, MaxScore / 2) })
            .OrderByDescending(s => s.Score)
            .Select(s => s.Node)
            .ToList();
    }


    private async Task IncrementScoreAsync(string redisKey, string node, int delta)
    {
        var newScore = await cache.SortedSetIncrementAsync(redisKey, node, delta);
        var clamped = Math.Clamp((int)newScore, MinScore, MaxScore);
        await cache.SortedSetAddAsync(redisKey, node, clamped);
    }
}