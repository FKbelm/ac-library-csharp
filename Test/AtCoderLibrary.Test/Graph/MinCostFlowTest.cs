﻿using FluentAssertions;
using MersenneTwister;
using Xunit;

namespace AtCoder
{
    public class MinCostFlowTest
    {
        [Fact]
        public void Simple()
        {
            var g = new McfGraphInt(4);
            g.AddEdge(0, 1, 1, 1);
            g.AddEdge(0, 2, 1, 1);
            g.AddEdge(1, 3, 1, 1);
            g.AddEdge(2, 3, 1, 1);
            g.AddEdge(1, 2, 1, 1);
            g.Slope(0, 3, 10).Should().Equal(new[] { (0, 0), (2, 4) });

            McfGraphInt.Edge e;

            e = new McfGraphInt.Edge(0, 1, 1, 1, 1);
            g.GetEdge(0).Should().Be(e);
            e = new McfGraphInt.Edge(0, 2, 1, 1, 1);
            g.GetEdge(1).Should().Be(e);
            e = new McfGraphInt.Edge(1, 3, 1, 1, 1);
            g.GetEdge(2).Should().Be(e);
            e = new McfGraphInt.Edge(2, 3, 1, 1, 1);
            g.GetEdge(3).Should().Be(e);
            e = new McfGraphInt.Edge(1, 2, 1, 0, 1);
            g.GetEdge(4).Should().Be(e);
        }
        [Fact]
        public void Usage()
        {
            {
                var g = new McfGraphInt(2);
                g.AddEdge(0, 1, 1, 2);
                g.Flow(0, 1).Should().Be((1, 2));
            }
            {
                var g = new McfGraphInt(2);
                g.AddEdge(0, 1, 1, 2);
                g.Slope(0, 1).Should().Equal(new[] { (0, 0), (1, 2) });
            }
        }
        [Fact]
        public void OutOfRange()
        {
            var g = new McfGraphInt(10);
            g.Invoking(g => g.Slope(-1, 3)).Should().ThrowDebugAssertIfDebug();
            g.Invoking(g => g.Slope(3, 3)).Should().ThrowDebugAssertIfDebug();
        }

        [Fact]
        public void SelfLoop()
        {
            var g = new McfGraphInt(3);
            g.AddEdge(0, 0, 100, 123).Should().Be(0);

            McfGraphInt.Edge e = new McfGraphInt.Edge(0, 0, 100, 0, 123);
            g.GetEdge(0).Should().Be(e);
        }

        [Fact]
        public void SameCostPaths()
        {
            var g = new McfGraphInt(3);
            g.AddEdge(0, 1, 1, 1).Should().Be(0);
            g.AddEdge(1, 2, 1, 0).Should().Be(1);
            g.AddEdge(0, 2, 2, 1).Should().Be(2);
            g.Slope(0, 2).Should().Equal(new[] { (0, 0), (3, 3) });
        }

        [Fact]
        public void Invalid()
        {
            var g = new McfGraphInt(2);
            g.Invoking(g => g.AddEdge(0, 0, -1, 0)).Should().ThrowDebugAssertIfDebug();
            g.Invoking(g => g.AddEdge(0, 0, 0, -1)).Should().ThrowDebugAssertIfDebug();
        }

        [Fact]
        public void Stress()
        {
            var mt = MTRandom.Create();
            for (int phase = 0; phase < 1000; phase++)
            {
                int n = mt.Next(2, 21);
                int m = mt.Next(1, 101);
                var (s, t) = mt.NextPair(0, n);
                if (mt.NextBool()) (s, t) = (t, s);

                var gMf = new MFGraphInt(n);
                var g = new McfGraphInt(n);
                for (int i = 0; i < m; i++)
                {
                    int u = mt.Next(0, n);
                    int v = mt.Next(0, n);
                    int cap = mt.Next(0, 11);
                    int costIn = mt.Next(0, 10001);
                    g.AddEdge(u, v, cap, costIn);
                    gMf.AddEdge(u, v, cap);
                }
                var (flow, cost) = g.Flow(s, t);
                gMf.Flow(s, t).Should().Be(flow);

                int cost2 = 0;
                var vCap = new int[n];
                foreach (var e in g.Edges())
                {
                    vCap[e.From] -= e.Flow;
                    vCap[e.To] += e.Flow;
                    cost2 += e.Flow * e.Cost;
                }
                cost.Should().Be(cost2);

                for (int i = 0; i < n; i++)
                {
                    if (i == s)
                    {
                        (-flow).Should().Be(vCap[i]);
                    }
                    else if (i == t)
                    {
                        flow.Should().Be(vCap[i]);
                    }
                    else
                    {
                        vCap[i].Should().Be(0);
                    }
                }

                // check: there is no negative-cycle
                var dist = new int[n];
                while (true)
                {
                    bool update = false;
                    foreach (var e in g.Edges())
                    {
                        if (e.Flow < e.Cap)
                        {
                            int ndist = dist[e.From] + e.Cost;
                            if (ndist < dist[e.To])
                            {
                                update = true;
                                dist[e.To] = ndist;
                            }
                        }
                        if (e.Flow != 0)
                        {
                            int ndist = dist[e.To] - e.Cost;
                            if (ndist < dist[e.From])
                            {
                                update = true;
                                dist[e.From] = ndist;
                            }
                        }
                    }
                    if (!update) break;
                }
            }
        }
    }
}
