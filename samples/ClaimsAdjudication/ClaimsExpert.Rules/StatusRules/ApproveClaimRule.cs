﻿using NRules.Fluent.Dsl;
using NRules.Samples.ClaimsExpert.Domain;

namespace NRules.Samples.ClaimsExpert.Rules.StatusRules
{
    [Name("Approve claim")]
    [Priority(1000)]
    public class ApproveClaimRule : Rule
    {
        public override void Define()
        {
            Claim claim = null;

            When()
                .Match<Claim>(() => claim, c => c.Status == ClaimStatus.Open)
                .Not<ClaimAlert>(ce => ce.Claim == claim, ce => ce.Severity > 1);

            Then()
                .Do(ctx => Approve(claim));
        }

        private static ClaimStatus Approve(Claim claim)
        {
            return claim.Status = ClaimStatus.Approved;
        }
    }
}