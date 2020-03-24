using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using Wonka.BizRulesEngine;
using Wonka.BizRulesEngine.RuleTree;
using Wonka.BizRulesEngine.RuleTree.RuleTypes;
using Wonka.MetaData;
using Wonka.Product;
using WonkaSystem;

namespace WonkaRulesBlazorEditor.Extensions
{
    public static class BlazorAppWonkaExtensions
    {
		private static int mnRuleCounter    = 100000;
		private static int mnRuleSetCounter = 200000;

		public static void AddNewRule(this WonkaBizRuleSet poRuleSet,
			                           WonkaRefEnvironment poRefEnv,
			                                        string psAddRuleDesc,
													string psAddRuleTargetAttr,
													string psAddRuleTypeNum,
													string psAddRuleValue1,
													string psAddRuleValue2,
													  bool pbAddRuleNotOp = false)
		{
			int nRuleTypeNum = Int32.Parse(psAddRuleTypeNum);

		    WonkaBizRule NewRule = null;

			WonkaRefAttr targetAttr = poRefEnv.GetAttributeByAttrName(psAddRuleTargetAttr);

			if (nRuleTypeNum == 1)
			{
				if (!targetAttr.IsNumeric && !targetAttr.IsDecimal)
					throw new DataException("ERROR!  Cannot perform arithmetic limit on a non-numeric value.");

				double dMinVal = 0;
				double dMaxVal = 0;

				Double.TryParse(psAddRuleValue1, out dMinVal);
				Double.TryParse(psAddRuleValue2, out dMaxVal);

				NewRule =
					new ArithmeticLimitRule(mnRuleCounter++, TARGET_RECORD.TRID_NEW_RECORD, targetAttr.AttrId, dMinVal, dMaxVal);
			}
			else if (nRuleTypeNum == 2)
			{
				/*
				 * NOTE: Will handle ArithmeticRule later
				string[] asParamArray = new string[0];
				 */
			}
			else if (nRuleTypeNum == 3)
			{
				if (!String.IsNullOrEmpty(psAddRuleValue1))
				{
					NewRule =
						new AssignmentRule(mnRuleCounter++, TARGET_RECORD.TRID_NEW_RECORD, targetAttr.AttrId, psAddRuleValue1);
				}
			}
			else if (nRuleTypeNum == 4)
			{
				/*
				 * NOTE: Will handle CustomOperatorRule later
				 */
			}
			else if (nRuleTypeNum == 5)
			{
				if (!targetAttr.IsDate)
					throw new DataException("ERROR!  Cannot perform date limit on a non-date value.");

				if ((!String.IsNullOrEmpty(psAddRuleValue1)) || (!String.IsNullOrEmpty(psAddRuleValue2)))
				{
					DateTime dtMinTime = DateTime.MinValue;
					DateTime dtMaxTime = DateTime.MaxValue;

					if (!String.IsNullOrEmpty(psAddRuleValue1))
						DateTime.TryParse(psAddRuleValue1, out dtMinTime);

					if (!String.IsNullOrEmpty(psAddRuleValue2))
						DateTime.TryParse(psAddRuleValue2, out dtMaxTime);

					var DtLimitRule =
						new DateLimitRule(mnRuleCounter++) { MinValue = dtMinTime, MaxValue = dtMaxTime, TargetAttribute = targetAttr };

					NewRule = DtLimitRule;
				}
			}
			else if (nRuleTypeNum == 6)
			{
				if ((!String.IsNullOrEmpty(psAddRuleValue1)) || (!String.IsNullOrEmpty(psAddRuleValue2)))
				{
					HashSet<string> RuleDomain = new HashSet<string>();

					if (!String.IsNullOrEmpty(psAddRuleValue1))
					{
						if (psAddRuleValue1.Contains(","))
						{
							var DomainVals = psAddRuleValue1.Split(new char[1] { ',' });
							DomainVals.ToList().ForEach(x => DomainVals.Append(x));
						}
						else
							RuleDomain.Add(psAddRuleValue1);
					}

					if (!String.IsNullOrEmpty(psAddRuleValue2))
					{
						if (psAddRuleValue2.Contains(","))
						{
							var DomainVals = psAddRuleValue2.Split(new char[1] { ',' });
							DomainVals.ToList().ForEach(x => DomainVals.Append(x));
						}
						else
							RuleDomain.Add(psAddRuleValue2);
					}

					var DmnRule =
						new DomainRule(mnRuleCounter++, TARGET_RECORD.TRID_NEW_RECORD, targetAttr.AttrId, false);

					DmnRule.DomainCache = RuleDomain;

					NewRule = DmnRule;
				}
			}
			else if (nRuleTypeNum == 7)
			{
				NewRule = new PopulatedRule(mnRuleCounter++, TARGET_RECORD.TRID_NEW_RECORD, targetAttr.AttrId);
			}

			if (NewRule != null)
			{
				if (!String.IsNullOrEmpty(psAddRuleDesc))
				{
					NewRule.DescRuleId  = psAddRuleDesc;
				}

				NewRule.NotOperator = pbAddRuleNotOp;

				poRuleSet.AddRule(NewRule);
			}
		}

        public static void AddNewRuleSet(this WonkaBizRuleSet poRuleSet,			                           
			                                           string psAddRuleSetDesc,
													   string psAddRuleSetTypeNum,
													   string psAddRuleSetErrorLvlNum)
		{
			int nRuleSetTypeNum   = Int32.Parse(psAddRuleSetTypeNum);
			int nRuleSetErrLvlNum = Int32.Parse(psAddRuleSetErrorLvlNum);

			WonkaBizRuleSet NewRuleSet = null;

			RULE_OP          rulesOp  = RULE_OP.OP_NONE;
			RULE_SET_ERR_LVL errLevel = RULE_SET_ERR_LVL.ERR_LVL_NONE;

			if (nRuleSetTypeNum == 1)
				rulesOp = RULE_OP.OP_AND;
			else
				rulesOp = RULE_OP.OP_OR;

			if (nRuleSetErrLvlNum == 1)
				errLevel = RULE_SET_ERR_LVL.ERR_LVL_WARNING;
			else
				errLevel = RULE_SET_ERR_LVL.ERR_LVL_SEVERE;

			NewRuleSet = new WonkaBizRuleSet(mnRuleSetCounter++);
			NewRuleSet.RulesEvalOperator = rulesOp;
			NewRuleSet.ErrorSeverity     = errLevel;

			poRuleSet.AddChildRuleSet(NewRuleSet);
		}

		public static WonkaBizRuleSet FindRuleSet(this WonkaBizRulesEngine rulesEngine, int pnTargetRuleSetId)
		{
			WonkaBizRuleSet FoundRuleSet = new WonkaBizRuleSet();

			if (rulesEngine.RuleTreeRoot.RuleSetId == pnTargetRuleSetId)
				return rulesEngine.RuleTreeRoot;

			foreach (var TmpChildSet in rulesEngine.RuleTreeRoot.ChildRuleSets)
			{
				if (TmpChildSet.RuleSetId == pnTargetRuleSetId)
					return TmpChildSet;
				else
				{
					FoundRuleSet = FindRuleSet(TmpChildSet, pnTargetRuleSetId);
					if (IsValidRuleSet(FoundRuleSet))
						break;
				}
			}

			return FoundRuleSet;
		}

		public static WonkaBizRuleSet FindRuleSet(this WonkaBizRuleSet poTargetSet, int pnTargetRuleSetId)
		{
			WonkaBizRuleSet FoundRuleSet = new WonkaBizRuleSet();

			foreach (var TmpChildSet in poTargetSet.ChildRuleSets)
			{
				if (TmpChildSet.RuleSetId == pnTargetRuleSetId)
					return TmpChildSet;
				else
				{
					FoundRuleSet = FindRuleSet(TmpChildSet, pnTargetRuleSetId);
					if (IsValidRuleSet(FoundRuleSet))
						break;
				}
			}

			return FoundRuleSet;
		}

		public static WonkaBizRuleSet FindRuleSet(this WonkaBizRulesEngine rulesEngine, string psSoughtDescName)
		{
			WonkaBizRuleSet FoundRuleSet = new WonkaBizRuleSet();

			if (rulesEngine.RuleTreeRoot.Description == psSoughtDescName)
				return rulesEngine.RuleTreeRoot;

			foreach (var TmpChildSet in rulesEngine.RuleTreeRoot.ChildRuleSets)
			{
				if (TmpChildSet.Description == psSoughtDescName)
					return TmpChildSet;
				else
				{
					FoundRuleSet = FindRuleSet(TmpChildSet, psSoughtDescName);
					if (IsValidRuleSet(FoundRuleSet))
						break;
				}
			}

			return FoundRuleSet;
		}

		public static WonkaBizRuleSet FindRuleSet(this WonkaBizRuleSet poTargetSet, string psSoughtDescName)
		{
			WonkaBizRuleSet FoundRuleSet = new WonkaBizRuleSet();

			foreach (var TmpChildSet in poTargetSet.ChildRuleSets)
			{
				if (TmpChildSet.Description == psSoughtDescName)
					return TmpChildSet;
				else
				{
					FoundRuleSet = FindRuleSet(TmpChildSet, psSoughtDescName);
					if (IsValidRuleSet(FoundRuleSet))
						break;
				}
			}

			return FoundRuleSet;
		}

		public static WonkaBizRule FindRuleById(this WonkaBizRulesEngine rulesEngine, string psSoughtId)
		{
			WonkaBizRule FoundRule = new ArithmeticRule();

			foreach (var TmpChildSet in rulesEngine.RuleTreeRoot.ChildRuleSets)
			{
				foreach (var TmpRule in TmpChildSet.EvaluativeRules)
				{
					if (TmpRule.DescRuleId == psSoughtId)
						return TmpRule;
					else
					{
						FoundRule = FindRuleById(TmpChildSet, psSoughtId);
						if (FoundRule.RuleId > 0)
							return FoundRule;
					}
				}

				foreach (var TmpRule in TmpChildSet.AssertiveRules)
				{
					if (TmpRule.DescRuleId == psSoughtId)
						return TmpRule;
					else
					{
						FoundRule = FindRuleById(TmpChildSet, psSoughtId);
						if (FoundRule.RuleId > 0)
							return FoundRule;
					}
				}

				FoundRule = FindRuleById(TmpChildSet, psSoughtId);
				if (FoundRule.RuleId > 0)
					return FoundRule;
			}

			return FoundRule;
		}

		public static WonkaBizRule FindRuleById(this WonkaBizRuleSet poTargetSet, string psSoughtId)
		{
			WonkaBizRule FoundRule = new ArithmeticRule();

			foreach (var TmpChildSet in poTargetSet.ChildRuleSets)
			{
				foreach (var TmpRule in TmpChildSet.EvaluativeRules)
				{
					if (TmpRule.DescRuleId == psSoughtId)
						return TmpRule;
					else
					{
						FoundRule = FindRuleById(TmpChildSet, psSoughtId);
						if (FoundRule.RuleId > 0)
							return FoundRule;
					}
				}

				foreach (var TmpRule in TmpChildSet.AssertiveRules)
				{
					if (TmpRule.DescRuleId == psSoughtId)
						return TmpRule;
					else
					{
						FoundRule = FindRuleById(TmpChildSet, psSoughtId);
						if (FoundRule.RuleId > 0)
							return FoundRule;
					}
				}

				FoundRule = FindRuleById(TmpChildSet, psSoughtId);
				if (FoundRule.RuleId > 0)
					return FoundRule;
			}

			return FoundRule;
		}

		public static string GetOpTypeDesc(this ArithmeticRule poTargetRule)
		{
			string sOpTypeDesc = "";

			if (poTargetRule.OpType == ARITH_OP_TYPE.AOT_SUM)
				sOpTypeDesc = "ADD";
			else if (poTargetRule.OpType == ARITH_OP_TYPE.AOT_DIFF)
				sOpTypeDesc = "MINUS";
			else if (poTargetRule.OpType == ARITH_OP_TYPE.AOT_PROD)
				sOpTypeDesc = "MULTIPLY";
			else if (poTargetRule.OpType == ARITH_OP_TYPE.AOT_QUOT)
				sOpTypeDesc = "DIVIDE";

			return sOpTypeDesc;
		}

		public static string GetRuleDescription(this WonkaBizRule targetRule)
		{
			StringBuilder RuleDesc = new StringBuilder("Rule:" + targetRule.DescRuleId + ":");

			if (targetRule is ArithmeticLimitRule)
			{
				RuleDesc.Append(" (ArithLimit -> " + targetRule.TargetAttribute.AttrName);
			}
			else if (targetRule is ArithmeticRule)
			{
				RuleDesc.Append(" (ArithAssign -> " + targetRule.TargetAttribute.AttrName);
			}
			else if (targetRule is AssignmentRule)
			{
				RuleDesc.Append(" (AssignRule -> " + targetRule.TargetAttribute.AttrName);
			}
			else if (targetRule is CustomOperatorRule)
			{
				RuleDesc.Append(" (CustomOpRule -> " + targetRule.TargetAttribute.AttrName);
			}
			else if (targetRule is DateLimitRule)
			{
				RuleDesc.Append(" (DateLimitRule -> " + targetRule.TargetAttribute.AttrName);
			}
			else if (targetRule is DomainRule)
			{
				RuleDesc.Append(" (DomainRule) -> " + targetRule.TargetAttribute.AttrName);
			}
			else if (targetRule is PopulatedRule)
			{
				RuleDesc.Append(" (PopulatedRule -> " + targetRule.TargetAttribute.AttrName);
			}

			RuleDesc.Append(")");

			return RuleDesc.ToString();
		}

		public static string GetRuleDetails(this WonkaBizRule targetRule)
		{
			StringBuilder RuleDesc = new StringBuilder();

			if (targetRule is ArithmeticLimitRule)
			{
				var ArithLimitRule = (ArithmeticLimitRule) targetRule;

				if (targetRule.NotOperator)
					RuleDesc.Append("NOT (");

				RuleDesc.Append(ArithLimitRule.MinValue + " > " + targetRule.TargetAttribute.AttrName + " > " + ArithLimitRule.MaxValue);

				if (targetRule.NotOperator)
					RuleDesc.Append(")");
			}
			else if (targetRule is ArithmeticRule)
			{
				var ArithAssignRule = (ArithmeticRule) targetRule;

				RuleDesc.Append(ArithAssignRule.TargetAttribute.AttrName + " = " +
					            ArithAssignRule.GetOpTypeDesc() + "(" + String.Join(",", ArithAssignRule.DomainCache) + ")");
			}
			else if (targetRule is AssignmentRule)
			{
				var AssignRule = (AssignmentRule) targetRule;
				RuleDesc.Append(AssignRule.TargetAttribute.AttrName + " = (" + AssignRule.AssignValue + ")");
			}
			else if (targetRule is CustomOperatorRule)
			{
				var CustomOpRule = (CustomOperatorRule) targetRule;
				RuleDesc.Append(CustomOpRule.TargetAttribute.AttrName + " = " + CustomOpRule.CustomOpName + "(" + String.Join(",", (CustomOpRule.CustomOpPropArgs)) + ")");
			}
			else if (targetRule is DateLimitRule)
			{
				var DateLmtRule = (DateLimitRule) targetRule;

				if (targetRule.NotOperator)
					RuleDesc.Append("NOT (");

				RuleDesc.Append(DateLmtRule.MinValue + " > " + targetRule.TargetAttribute.AttrName + " > " + DateLmtRule.MaxValue);

				if (targetRule.NotOperator)
					RuleDesc.Append(")");
			}
			else if (targetRule is DomainRule)
			{
				string sNot = (targetRule.NotOperator) ? "NOT" : "";

				var DmnRule = (DomainRule) targetRule;
				RuleDesc.Append(DmnRule.TargetAttribute.AttrName + " " + sNot + " IN (" + String.Join(",", DmnRule.DomainCache) + ")");
			}
			else if (targetRule is PopulatedRule)
			{
				string sNot = (targetRule.NotOperator) ? "NOT" : "";

				var PopRule = (PopulatedRule) targetRule;
				RuleDesc.Append(PopRule.TargetAttribute.AttrName + " IS " + sNot + " POPULATED");
			}

			return RuleDesc.ToString();
		}

		public static string GetRuleSetDescription(this WonkaBizRuleSet targetRuleSet)
		{
			StringBuilder RuleSetDesc = new StringBuilder(targetRuleSet.Description);

			return RuleSetDesc.ToString();
		}

		public static bool IsValidRuleSet(this WonkaBizRuleSet poTargetRuleSet)
		{
			return (poTargetRuleSet.ChildRuleSets.Count > 0) || (poTargetRuleSet.AssertiveRules.Count > 0) || (poTargetRuleSet.EvaluativeRules.Count > 0);
		}
	}
}