using Flurl;

namespace Train.Solver.Util.Helpers;

public static class LogoHelpers
{
    private const string GithubUserContentUrl = "https://raw.githubusercontent.com";

    public static string BuildGithubLogoUrl(string logoPath)
    {
        return GithubUserContentUrl.AppendPathSegment(logoPath, false).ToString();
    }
}
