using System.Collections.Generic;

[System.Serializable]
public class RankingSnapshot
{
    public string snapshotDate;

    public Dictionary<string, int> levelRanking =
        new Dictionary<string, int>();

    public Dictionary<string, int> questRanking =
        new Dictionary<string, int>();
}