using System.Collections.Generic;
using UnityEngine;

namespace ProjectScim
{
    /// <summary>
    /// The supply-chain state the simulation acts out. Numbers come from the Mise
    /// agent system (see mise/INTEGRATION.md) and are the real fixture values, not
    /// illustrative ones: Downtown holds 4.0 kg against a par of 40.0 (36 short),
    /// Marina holds 34.0 against a par of 24.0 (10 surplus, 2 days to expiry), so
    /// the agents transfer 10 and buy the net 26 at $2.05/kg = $53.30.
    /// </summary>
    public static class MiseScenario
    {
        public const string Product = "Roma tomatoes";
        public const string Sku = "TOM-ROMA";
        public const string Supplier = "Bay Foods Wholesale";

        public const float TransferQty = 10f;
        public const float PurchaseQty = 26f;
        public const float UnitPrice = 2.05f;
        public static float PurchaseTotal => Mathf.Round(PurchaseQty * UnitPrice * 100f) / 100f;

        public class Branch
        {
            public string Name;
            public float OnHand;
            public float Par;
            public float Reorder;
            public int ExpiryDays;
            public float DailyBurn;
            public Vector3 Origin;      // where this branch sits in the world

            public float Shortfall => Mathf.Max(0f, Par - OnHand);
            public float Surplus => Mathf.Max(0f, OnHand - Par);
            public bool IsShort => OnHand < Reorder || Shortfall > 0.01f;
            public float DaysOfCover => DailyBurn > 0f ? OnHand / DailyBurn : 999f;
        }

        /// <summary>Fresh copy of the opening state, so the sim can be reset.</summary>
        public static List<Branch> NewBranches()
        {
            return new List<Branch>
            {
                new Branch { Name = "Downtown", OnHand = 4f,  Par = 40f, Reorder = 12f,
                             ExpiryDays = 4, DailyBurn = 14.16f, Origin = new Vector3(0f, 0f, 0f) },
                // Kept tight in x so all three branches sit between the HUD panels
                // rather than under them.
                new Branch { Name = "Marina",   OnHand = 34f, Par = 24f, Reorder = 8f,
                             ExpiryDays = 2, DailyBurn = 10.1f, Origin = new Vector3(-19f, 0f, 15f) },
                new Branch { Name = "Mission",  OnHand = 16f, Par = 20f, Reorder = 6f,
                             ExpiryDays = 5, DailyBurn = 8.1f,  Origin = new Vector3(19f, 0f, 15f) },
            };
        }

        public static readonly Vector3 WarehouseOrigin = new Vector3(0f, 0f, 40f);

        /// <summary>
        /// Why Mission cannot be the donor even though it holds 16 kg — it is itself
        /// below par. Worth showing: it proves the decision was not trivial.
        /// </summary>
        public const string DecisionRationale =
            "Marina holds 10 kg above par with 2 days to expiry; Downtown is below reorder point. " +
            "Mission is also below par, so it cannot donate. Transfer nearest-expiry stock first, " +
            "then buy only the net shortfall.";
    }
}
