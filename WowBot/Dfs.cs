using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WowBot
{
    public static class ContinentID
    {
        public const int EasternKingdoms = 0;
        public const int Kalimdor = 1;
        public const int Outland = 530;
        public const int Northrend = 571;
    }

    public class Graph
    {
        private Dictionary<int, List<int>> graph = new Dictionary<int, List<int>>();
        private Dictionary<int, List<int>> nodeHistory = new Dictionary<int, List<int>>();
        private int currStartId = 0;
        private bool targetFound = false;
        private Dictionary<int, HashSet<int>> foundTargets = new Dictionary<int, HashSet<int>>();
        public bool ShouldPrint { get; set; } = false;

        public void AddEdge(int u, int v)
        {
            if (!graph.ContainsKey(u))
                graph[u] = new List<int>();
            graph[u].Add(v);
        }

        private void DFSUtil(int v, HashSet<int> visited, int targetV)
        {
            visited.Add(v);
            if (!nodeHistory.ContainsKey(currStartId))
                nodeHistory[currStartId] = new List<int>();
            if (!nodeHistory[currStartId].Contains(v) && v != currStartId)
                nodeHistory[currStartId].Add(v);

            if (v == targetV)
            {
                if (ShouldPrint)
                    Console.WriteLine($"{v} Target found!");
                targetFound = true;
                if (!foundTargets.ContainsKey(targetV))
                    foundTargets[targetV] = new HashSet<int>();
                foreach (var item in visited)
                    foundTargets[targetV].Add(item);
                return;
            }
            else if (!targetFound)
            {
                if (ShouldPrint)
                    Console.Write($"{v} ");
#if NETCOREAPP
                foreach (var neighbour in graph.GetValueOrDefault(v, new List<int>()))
#else
                List<int> neighbours;
                graph.TryGetValue(v, out neighbours); // incase getvalueordefault for dict is not available
                foreach (var neighbour in neighbours) // incase getvalueordefault for dict is not available
#endif
                {
                    if (!visited.Contains(neighbour))
                        DFSUtil(neighbour, visited, targetV);
                }
            }
        }

        public bool DFS_search(int startId, int targetId)
        {
            if (ShouldPrint)
                Console.WriteLine($"start_id: {startId}, target_id: {targetId}");
            currStartId = startId;
            var visited = new HashSet<int>();

            if (foundTargets.ContainsKey(targetId) && foundTargets[targetId].Contains(startId))
                return true;
            else if (nodeHistory.ContainsKey(startId))
            {
                if (foundTargets.ContainsKey(targetId))
                {
                    foreach (var startNode in nodeHistory[startId])
                    {
                        if (startNode == targetId || foundTargets[targetId].Contains(startNode))
                            return true;
                    }
                }
                else
                {
                    startId = nodeHistory[startId].Last();
                    if (nodeHistory.ContainsKey(startId))
                        foreach (var item in nodeHistory[startId])
                            visited.Add(item);
                }
            }

            targetFound = false;
#if NETCOREAPP
            foreach (var vertex in graph.GetValueOrDefault(startId, new List<int>()))
#else
            List<int> vertexes;
            graph.TryGetValue(startId, out vertexes); // incase getvalueordefault for dict is not available
            foreach (var vertex in vertexes)
#endif
            {
                if (!visited.Contains(vertex) && !targetFound)
                    DFSUtil(vertex, visited, targetId);

                if (targetFound)
                    return true;
            }
            return targetFound;
        }
    }

    public static class Dfs
    {
        // Settings
        private const int TRICKERER_SQL = 1;
        private static int continent = ContinentID.Northrend;

        private static List<string> ReadLinesFromFile(string filename)
        {
            return File.ReadAllLines(filename).ToList();
        }

        private static string ExtractIndex(string inputStr, char sep, int idx)
        {
            return inputStr.Split(sep)[idx];
        }

        private static readonly List<Tuple<int, int>> isolatedNodes = new List<Tuple<int, int>>
        {
            Tuple.Create(3923, 3936),
            Tuple.Create(3959, 3978),
            Tuple.Create(4087, 4103),
            Tuple.Create(4746, 4789),
            Tuple.Create(4790, 4854)
        };

        private static bool IsIsolatedNodes(int nodeId, int otherNodeId)
        {
            foreach (var nodeRange in isolatedNodes)
            {
                if (nodeId >= nodeRange.Item1 && nodeId <= nodeRange.Item2)
                    return !(otherNodeId >= nodeRange.Item1 && otherNodeId <= nodeRange.Item2);
                else if (otherNodeId >= nodeRange.Item1 && otherNodeId <= nodeRange.Item2)
                    return !(nodeId >= nodeRange.Item1 && nodeId <= nodeRange.Item2);
            }
            return false;
        }

        public static void StartDfs()
        {
            var nodeMapId = continent.ToString();
            var nodeLines = new List<string>();
            var nodeZones = new Dictionary<int, int>();
            var isolatedZones = new List<int> { 141, 1657 };
            var nodeVertices = new Dictionary<int, List<int>>();

            if (continent < 2)
                nodeLines = ReadLinesFromFile("2023_04_04_00_creature_template_npcbot_wander_nodes.sql");
            else
            {
                if (TRICKERER_SQL == 1)
                    nodeLines = ReadLinesFromFile("trickerer_outland_northrend.sql");
                else
                    nodeLines = ReadLinesFromFile("2023_06_09_00_creature_template_npcbot_wander_nodes.sql");
            }

            foreach (var line in nodeLines)
            {
                if (!string.IsNullOrEmpty(line) && line[0] == '(')
                {
                    var mapId = ExtractIndex(line, ',', 2).Trim();
                    if (mapId == nodeMapId)
                    {
                        var nodeId = int.Parse(line.Split(',')[0].Substring(1).Trim());
                        var nodeLinks = Regex.Replace(ExtractIndex(line, '\'', 3), ":0", "");
                        nodeLinks.Replace(" ", "");
                        //var links = nodeLinks.Split(' ').Select(int.Parse).ToList();
                        var links = nodeLinks.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                         .Where(s => int.TryParse(s, out _))
                         .Select(int.Parse)
                         .ToList();
                        nodeVertices[nodeId] = links;
                        var zoneId = int.Parse(ExtractIndex(line, ',', 3));
                        nodeZones[nodeId] = zoneId;
                    }
                }
            }

            var g = new Graph();
            foreach (var kvp in nodeVertices)
            {
                foreach (var link in kvp.Value)
                    g.AddEdge(kvp.Key, link);
            }

            g.ShouldPrint = true;
            if (continent == ContinentID.EasternKingdoms)
            {
                Debug.Assert(g.DFS_search(686, 913));
                Debug.Assert(g.DFS_search(748, 942));
                Debug.Assert(g.DFS_search(778, 2));
            }
            else if (continent == ContinentID.Kalimdor)
            {
                Debug.Assert(g.DFS_search(1005, 1236));
                Debug.Assert(g.DFS_search(26, 2366));
                Debug.Assert(g.DFS_search(1271, 95));
            }
            else if (TRICKERER_SQL == 1)
            {
                if (continent == ContinentID.Outland)
                {
                    Debug.Assert(g.DFS_search(2418, 2864));
                    Debug.Assert(g.DFS_search(2889, 3096));
                    Debug.Assert(g.DFS_search(3257, 3442));
                }
                else
                {
                    Debug.Assert(g.DFS_search(4010, 4556));
                    Debug.Assert(g.DFS_search(4992, 4362));
                    Debug.Assert(g.DFS_search(3779, 5038));
                }
            }
            else
            {
                if (continent == ContinentID.Outland)
                {
                    Debug.Assert(g.DFS_search(2418, 2474));
                    Debug.Assert(g.DFS_search(2418, 2450));
                    Debug.Assert(g.DFS_search(2500, 2602));
                }
                else
                {
                    Debug.Assert(g.DFS_search(2802, 2900));
                    Debug.Assert(g.DFS_search(3273, 3330));
                    Debug.Assert(g.DFS_search(3353, 2708));
                }
            }

            int nodeCount = nodeVertices.Count;
            Console.WriteLine($"\nLooping all nodes... Nodes: {nodeCount}");
            bool linksToAll = true;
            bool breakWhenNoLink = false;
            int loopCounter = 0;
            int isolatedCounter = 0;
            g.ShouldPrint = false;

            foreach (var nodeId in nodeVertices.Keys)
            {
                foreach (var otherNodeId in nodeVertices.Keys)
                {
                    bool tryingToReachIsolated;
                    if (TRICKERER_SQL == 1)
                        tryingToReachIsolated = (isolatedZones.Contains(nodeZones[nodeId]) || isolatedZones.Contains(nodeZones[otherNodeId])) && nodeZones[nodeId] != nodeZones[otherNodeId] || IsIsolatedNodes(nodeId, otherNodeId);
                    else
                        tryingToReachIsolated = (isolatedZones.Contains(nodeZones[nodeId]) || isolatedZones.Contains(nodeZones[otherNodeId])) && nodeZones[nodeId] != nodeZones[otherNodeId];

                    if (tryingToReachIsolated)
                        isolatedCounter++;
                    else if (nodeId != otherNodeId && !tryingToReachIsolated)
                    {
                        bool canReach = g.DFS_search(nodeId, otherNodeId);
                        loopCounter++;
                        if (!canReach)
                        {
                            Console.WriteLine($"CAN'T REACH: {otherNodeId} (zone: {nodeZones[otherNodeId]}) FROM NODE: {nodeId} (zone: {nodeZones[nodeId]})");
                            linksToAll = false;
                            if (breakWhenNoLink)
                            {
                                break;
                            }
                        }
                    }
                }
                if (!linksToAll && breakWhenNoLink)
                {
                    break;
                }
            }

            Console.WriteLine($"Done checking links... links_to_all: {linksToAll}. Nodes checked: {loopCounter + isolatedCounter} - should be {nodeCount} * {nodeCount - 1} = {nodeCount * (nodeCount - 1)}");
            Console.WriteLine($"isolated_counter: {isolatedCounter} ({(double)isolatedCounter / loopCounter * 100} %)");
        }
    }
}