using System.Collections.Generic;
using System;
using UnityEngine;

public static class PotTesting
{
    // ---------- Public entry point ----------
    public static void RunAll()
    {
        int passed = 0;
        int total = 8;

        if (Case01()) passed++;
        if (Case02()) passed++;
        if (Case03()) passed++;
        if (Case04()) passed++;
        if (Case05()) passed++;
        if (Case06()) passed++;
        if (Case07()) passed++;
        if (Case08_ShortStackTie_TrickleDown()) passed++;

        Debug.Log($"[PotPayoutSelfTest] {passed}/{total} cases passed.");
    }

    // ---------- Tiny assertion helper ----------
    private static bool CheckPayouts(
        string label,
        Dictionary<string, int> actual,
        params (Player P, int Chips)[] expectation)
    {
        foreach (var (p, chips) in expectation)
        {
            int got = actual.TryGetValue(p.ID, out int v) ? v : 0;
            if (got != chips)
            {
                Debug.LogError(
                    $"[{label}] Player {p.PlayerName} expected {chips} but got {got}");
                return false;
            }
        }
        return true;
    }

    private static Player MakePlayer(string name, int stack = 2_000) =>
        new Player(stack, name, null);

    // ---------- Individual cases ----------
    private static bool Case01()
    {
        var label = "Case01_SingleWinner_NoSidePots";
        var a = MakePlayer("A"); var b = MakePlayer("B");
        var c = MakePlayer("C"); var d = MakePlayer("D");

        var pot = new Pot();
        pot.AddContribution(a, 500);
        pot.AddContribution(b, 500);
        pot.AddContribution(c, 500);
        pot.AddContribution(d, 500);

        var winners = new List<List<Player>> { new() { b } };
        var payouts = pot.PayoutWinners(winners);

        bool ok = CheckPayouts(label, payouts, (b, 2_000));
        LogResult(label, ok);
        return ok;
    }

    private static bool Case02()
    {
        var label = "Case02_TwoWayTie_NoSidePots";
        var a = MakePlayer("A"); var b = MakePlayer("B");
        var c = MakePlayer("C"); var d = MakePlayer("D");

        var pot = new Pot();
        foreach (var p in new[] { a, b, c, d }) pot.AddContribution(p, 500);

        var winners = new List<List<Player>> { new() { b, c } };
        var payouts = pot.PayoutWinners(winners);

        bool ok = CheckPayouts(label, payouts, (b, 1_000), (c, 1_000));
        LogResult(label, ok);
        return ok;
    }

    private static bool Case03()
    {
        var label = "Case03_ShortAllIn_SingleSidePot";
        var a = MakePlayer("A"); var b = MakePlayer("B");
        var c = MakePlayer("C"); var d = MakePlayer("D");

        var pot = new Pot();
        pot.AddContribution(a, 200);
        pot.AddContribution(b, 500);
        pot.AddContribution(c, 500);
        pot.AddContribution(d, 500);

        var winners = new List<List<Player>>
            { new() { b }, new() { d }, new() { c }, new() { a } };
        var payouts = pot.PayoutWinners(winners);

        bool ok = CheckPayouts(label, payouts, (b, 1_700));
        LogResult(label, ok);
        return ok;
    }

    private static bool Case04()
    {
        var label = "Case04_TieThenSidePots";
        var a = MakePlayer("A"); var b = MakePlayer("B");
        var c = MakePlayer("C"); var d = MakePlayer("D");

        var pot = new Pot();
        pot.AddContribution(a, 200);
        pot.AddContribution(b, 500);
        pot.AddContribution(c, 1_000);
        pot.AddContribution(d, 1_000);

        var winners = new List<List<Player>> { new() { b, c }, new() { d }, new() { a } };
        var payouts = pot.PayoutWinners(winners);

        bool ok = CheckPayouts(label, payouts, (c, 1_850), (b, 850));
        LogResult(label, ok);
        return ok;
    }

    private static bool Case05()
    {
        var label = "Case05_TinyAllIn_WinsMainPotOnly";
        var a = MakePlayer("A"); // best hand
        var b = MakePlayer("B"); var c = MakePlayer("C"); var d = MakePlayer("D");

        var pot = new Pot();
        pot.AddContribution(a, 200);
        pot.AddContribution(b, 500);
        pot.AddContribution(c, 500);
        pot.AddContribution(d, 500);

        var winners = new List<List<Player>>
            { new() { a }, new() { b }, new() { c }, new() { d } };
        var payouts = pot.PayoutWinners(winners);

        bool ok = CheckPayouts(label, payouts, (a, 800), (b, 900));
        LogResult(label, ok);
        return ok;
    }

    private static bool Case06()
    {
        var label = "Case06_ThreeWayTie_WithSidePot";
        var a = MakePlayer("A"); var b = MakePlayer("B"); var c = MakePlayer("C");
        var d = MakePlayer("D"); var e = MakePlayer("E");

        var pot = new Pot();
        pot.AddContribution(a, 225);
        pot.AddContribution(b, 225);
        pot.AddContribution(c, 225);
        pot.AddContribution(d, 400);
        pot.AddContribution(e, 400);

        var winners = new List<List<Player>> { new() { a, b, c }, new() { d }, new() { e } };
        var payouts = pot.PayoutWinners(winners);

        bool ok = CheckPayouts(label, payouts,
            (a, 375), (b, 375), (c, 375), (d, 350));
        LogResult(label, ok);
        return ok;
    }

    private static bool Case07()
    {
        var label = "Case07_OddChipRemainder";
        var a = MakePlayer("A"); var b = MakePlayer("B"); var c = MakePlayer("C");
        var d = MakePlayer("D"); var e = MakePlayer("E");

        var pot = new Pot();
        foreach (var p in new[] { a, b, c, d, e }) pot.AddContribution(p, 200);

        var winners = new List<List<Player>> { new() { a, b, c }, new() { d }, new() { e } };
        var payouts = pot.PayoutWinners(winners);

        bool ok = CheckPayouts(label, payouts, (a, 334), (b, 333), (c, 333));
        LogResult(label, ok);
        return ok;
    }

    private static bool Case08_ShortStackTie_TrickleDown()
    {
        var label = "Case08_ShortStackTie_TrickleDown";

        // Bob & Carol are tiny all-ins and tie for best hand
        var bob = MakePlayer("Bob");
        var carol = MakePlayer("Carol");
        var dave = MakePlayer("Dave");
        var alice = MakePlayer("Alice");

        var pot = new Pot();
        pot.AddContribution(bob, 300);
        pot.AddContribution(carol, 300);
        pot.AddContribution(dave, 800);
        pot.AddContribution(alice, 800);

        // Winners in ranking order: Bob & Carol tie ➜ Dave ➜ Alice
        var winners = new List<List<Player>>
    {
        new() { bob, carol },   // tie for best hand
        new() { dave },         // next best
        new() { alice }         // worst
    };

        var payouts = pot.PayoutWinners(winners);

        /* Expected:
           Main pot  (4 × 300 = 1 200) → Bob 600 | Carol 600
           Side pot  (2 × 500 = 1 000) → Dave 1 000
        */
        bool ok = CheckPayouts(label, payouts,
            (bob, 600),
            (carol, 600),
            (dave, 1_000));       // Alice gets 0, so no explicit check

        LogResult(label, ok);
        return ok;
    }

    // ---------- Utility ----------
    private static void LogResult(string label, bool ok)
    {
        Debug.Log(ok
            ? $"[PASS] {label}"
            : $"[FAIL] {label}");
    }
}