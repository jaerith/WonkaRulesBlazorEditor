using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

using Wonka.BizRulesEngine;
using Wonka.BizRulesEngine.Reporting;
using Wonka.BizRulesEngine.RuleTree;
using Wonka.BizRulesEngine.RuleTree.RuleTypes;
using Wonka.MetaData;

using WonkaRulesBlazorEditor.Data;

namespace WonkaRulesBlazorEditor.Extensions
{
    public static class BlazorAppWonkaExtensions
    {
		#region Constants

		public const string CONST_STATUS_CD_ACTIVE   = "ACT";
		public const string CONST_STATUS_CD_INACTIVE = "OOS";

		#endregion

		private static int mnAttrCounter    = 10000;
		private static int mnGroupCounter   = 20000;
		private static int mnFieldCounter   = 30000;
		private static int mnSourceCounter  = 40000;
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

					foreach (string sTmpValue in RuleDomain)
					{
						DmnRule.DomainValueProps.Add(sTmpValue, new WonkaBizRuleValueProps() { IsLiteralValue = true });
					}

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

				NewRule.ParentRuleSetId = poRuleSet.RuleSetId;
				NewRule.NotOperator     = pbAddRuleNotOp;

				poRuleSet.AddRule(NewRule);
			}
		}

		public static void AddNewNethereumRule(this WonkaBizRuleSet poRuleSet,
									            WonkaRefEnvironment poRefEnv,
												        	string psAddRuleDesc,
													        string psAddRuleTargetAttr,
													        string psAddRuleTypeNum,
															string psAddRuleEthAddress,
													        string psAddRuleValue1,
													        string psAddRuleValue2)
		{
			if (String.IsNullOrEmpty(psAddRuleTypeNum))
				psAddRuleTypeNum = "1";

			int nRuleTypeNum = Int32.Parse(psAddRuleTypeNum);

			WonkaBizRule NewRule = null;

			WonkaRefAttr targetAttr = poRefEnv.GetAttributeByAttrName(psAddRuleTargetAttr);

			if ((nRuleTypeNum == 1) | (nRuleTypeNum == 2) || (nRuleTypeNum == 5))
			{
				if (targetAttr.IsNumeric || targetAttr.IsDecimal)
					throw new DataException("ERROR!  Cannot perform offered Nethereum rules on a numeric value.");

				if (String.IsNullOrEmpty(psAddRuleEthAddress))
					psAddRuleEthAddress = BlazorAppNethereumExtensions.CONST_ETH_FNDTN_EOA_ADDRESS;
			}

			if (nRuleTypeNum == 1)
			{
				if (targetAttr.AttrName != "AccountStatus")
					throw new Exception("ERROR!  Cannot add BALANCE_WITHIN_RANGE rule with any attribute target other than AccountStatus.");

				WonkaBizSource DummySource =
					new WonkaBizSource("EF_EOA", psAddRuleEthAddress, "", "", "", "", "", null);

				CustomOperatorRule CustomOpRule =
					new CustomOperatorRule(mnRuleCounter++,
										   TARGET_RECORD.TRID_NEW_RECORD,
										   targetAttr.AttrId,
										   "BALANCE_WITHIN_RANGE",
										   BlazorAppNethereumExtensions.CheckBalanceIsWithinRange,
										   DummySource);

				CustomOpRule.AddDomainValue(psAddRuleEthAddress, true, TARGET_RECORD.TRID_NONE);
				CustomOpRule.AddDomainValue(psAddRuleValue1,     true, TARGET_RECORD.TRID_NONE);
				CustomOpRule.AddDomainValue(psAddRuleValue2,     true, TARGET_RECORD.TRID_NONE);

				NewRule = CustomOpRule;
			}
			else if (nRuleTypeNum == 2)
			{
				if (targetAttr.AttrName != "AuditReviewFlag")
					throw new Exception("ERROR!  Cannot add ANY_EVENTS_IN_BLOCK_RANGE rule with any attribute target other than AuditReviewFlag.");

				WonkaBizSource DummySource =
					new WonkaBizSource("ERC20", psAddRuleEthAddress, "", "", "", "", "", null);

				CustomOperatorRule CustomOpRule =
					new CustomOperatorRule(mnRuleCounter++,
										   TARGET_RECORD.TRID_NEW_RECORD,
										   targetAttr.AttrId,
										   "ANY_EVENTS_IN_BLOCK_RANGE",
										   BlazorAppNethereumExtensions.AnyEventsInBlockRange,
										   DummySource);

				if (String.IsNullOrEmpty(psAddRuleValue1))
					psAddRuleValue1 = "8450678";

				if (String.IsNullOrEmpty(psAddRuleValue2))
					psAddRuleValue2 = "8450698";

				CustomOpRule.AddDomainValue(psAddRuleEthAddress, true, TARGET_RECORD.TRID_NONE);
				CustomOpRule.AddDomainValue(psAddRuleValue1,     true, TARGET_RECORD.TRID_NONE);
				CustomOpRule.AddDomainValue(psAddRuleValue2,     true, TARGET_RECORD.TRID_NONE);

				NewRule = CustomOpRule;
			}
			else if (nRuleTypeNum == 3)
			{
				if (!targetAttr.IsNumeric && !targetAttr.IsDecimal)
					throw new DataException("ERROR!  Cannot perform arithmetic limit on a non-numeric value.");

				WonkaBizSource DummySource =
					new WonkaBizSource("ERC20", psAddRuleEthAddress, "", "", "", "", "", null);

				CustomOperatorRule CustomOpRule =
					new CustomOperatorRule(mnRuleCounter++,
										   TARGET_RECORD.TRID_NEW_RECORD,
										   targetAttr.AttrId,
										   "GET_ERC20_BALANCE",
										   BlazorAppNethereumExtensions.GetERC20Balance,
										   DummySource);

				CustomOpRule.AddDomainValue(psAddRuleEthAddress, true, TARGET_RECORD.TRID_NONE);
				CustomOpRule.AddDomainValue(psAddRuleValue1,     true, TARGET_RECORD.TRID_NONE);

				NewRule = CustomOpRule;
			}
			else if (nRuleTypeNum == 4)
			{
				if (!targetAttr.IsNumeric && !targetAttr.IsDecimal)
					throw new DataException("ERROR!  Cannot perform arithmetic limit on a non-numeric value.");

				WonkaBizSource DummySource =
					new WonkaBizSource("ContractName", psAddRuleEthAddress, "", "", "", "", "", null);

				CustomOperatorRule CustomOpRule =
					new CustomOperatorRule(mnRuleCounter++,
										   TARGET_RECORD.TRID_NEW_RECORD,
										   targetAttr.AttrId,
										   "CALL_SIMPLE_CONTRACT_METHOD",
										   BlazorAppNethereumExtensions.GetContractSimpleMethodValue,
										   DummySource);

				var sAltConfigIpfsUrl = psAddRuleValue1;
				var sFunctionName     = psAddRuleValue2;

				if (String.IsNullOrEmpty(sAltConfigIpfsUrl))
					sAltConfigIpfsUrl = BlazorAppNethereumExtensions.CONST_INFURA_IPFS_GATEWAY_URL + "/QmYDp4ocbF1AVSuY1zBhXa6P4c2oaPkHi2jaSE3HU6bQnQ";

				using (var client = new System.Net.Http.HttpClient())
				{
					var sConfigDataXml = client.GetStringAsync(sAltConfigIpfsUrl).Result;

					var configData = ReadConfigXml(sConfigDataXml);

					if (!String.IsNullOrEmpty(configData.HostUrl))
					    CustomOpRule.AddDomainValue(configData.HostUrl, true, TARGET_RECORD.TRID_NONE);
					else
						CustomOpRule.AddDomainValue(BlazorAppNethereumExtensions.CONST_TEST_INFURA_URL, true, TARGET_RECORD.TRID_NONE);

					CustomOpRule.AddDomainValue(configData.ContractABI, true, TARGET_RECORD.TRID_NONE);

					if (!String.IsNullOrEmpty(psAddRuleEthAddress))
						CustomOpRule.AddDomainValue(psAddRuleEthAddress, true, TARGET_RECORD.TRID_NONE);
					else
						CustomOpRule.AddDomainValue(configData.ContractAddress, true, TARGET_RECORD.TRID_NONE);

					CustomOpRule.AddDomainValue(sFunctionName, true, TARGET_RECORD.TRID_NONE);

					NewRule = CustomOpRule;
				}				
			}
			else if (nRuleTypeNum == 5)
			{
				if (targetAttr.AttrName != "AccountStatus")
					throw new Exception("ERROR!  Cannot add V rule with any attribute target other than AccountStatus.");

				WonkaBizSource DummySource =
					new WonkaBizSource("ContractName", psAddRuleEthAddress, "", "", "", "", "", null);

				CustomOperatorRule CustomOpRule =
					new CustomOperatorRule(mnRuleCounter++,
										   TARGET_RECORD.TRID_NEW_RECORD,
										   targetAttr.AttrId,
										   "VALIDATE_SIGNATURE",
										   BlazorAppNethereumExtensions.DetermineStatusByValidatingSignature,
										   DummySource);

				var sAttrTarget = psAddRuleValue1;
				var sSignature  = psAddRuleValue2;

				var TargetAttr  = poRefEnv.GetAttributeByAttrName(sAttrTarget);
				if (TargetAttr == null)
					throw new Exception("ERROR!  Cannot add rule since attribute(" + sAttrTarget + ") does not exist!");

				CustomOpRule.AddDomainValue(psAddRuleEthAddress, true, TARGET_RECORD.TRID_NONE);
				CustomOpRule.AddDomainValue(sAttrTarget,        false, TARGET_RECORD.TRID_NEW_RECORD);
				CustomOpRule.AddDomainValue(sSignature,          true, TARGET_RECORD.TRID_NONE);

				NewRule = CustomOpRule;
			}

			if (NewRule != null)
			{
				if (!String.IsNullOrEmpty(psAddRuleDesc))
					NewRule.DescRuleId = psAddRuleDesc;

				poRuleSet.AddRule(NewRule);
			}
		}		

		public static WonkaBizRuleSet AddNewRuleSet(this WonkaBizRuleSet poRuleSet,			                           
			                                                      string psAddRuleSetDesc,
													              string psAddRuleSetTypeNum,
													              string psAddRuleSetErrorLvlNum)
		{
			int nRuleSetTypeNum   = Int32.Parse(psAddRuleSetTypeNum);
			int nRuleSetErrLvlNum = Int32.Parse(psAddRuleSetErrorLvlNum);

			WonkaBizRuleSet NewRuleSet = null;

			RULE_OP          rulesOp  = RULE_OP.OP_NONE;
			RULE_SET_ERR_LVL errLevel = RULE_SET_ERR_LVL.ERR_LVL_NONE;

			if (String.IsNullOrEmpty(psAddRuleSetDesc))
				throw new DataException("ERROR!  Cannot add RuleSet without a description.");

			if (nRuleSetTypeNum == 1)
				rulesOp = RULE_OP.OP_AND;
			else
				rulesOp = RULE_OP.OP_OR;

			if (nRuleSetErrLvlNum == 1)
				errLevel = RULE_SET_ERR_LVL.ERR_LVL_WARNING;
			else
				errLevel = RULE_SET_ERR_LVL.ERR_LVL_SEVERE;

			NewRuleSet = new WonkaBizRuleSet(mnRuleSetCounter++) { Description = psAddRuleSetDesc };

			NewRuleSet.RulesEvalOperator = rulesOp;
			NewRuleSet.ErrorSeverity     = errLevel;

			poRuleSet.AddChildRuleSet(NewRuleSet);

			return NewRuleSet;
		}

		public static string GetErrors(this WonkaBizRuleTreeReport report)
		{
			var ErrorReport = new StringBuilder();

			foreach (var ReportNode in report.GetRuleSetSevereFailures())
			{
				if (ReportNode.RuleResults.Count > 0)
				{
					foreach (var RuleReportNode in ReportNode.RuleResults)
					{
						if (ErrorReport.Length > 0)
							ErrorReport.Append("\n");

						ErrorReport.Append(RuleReportNode.VerboseError.Replace("/", ""));
					}
				}
				else
					ErrorReport.Append(ReportNode.ErrorDescription);
			}

			return ErrorReport.ToString();
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
				}

				foreach (var TmpRule in TmpChildSet.AssertiveRules)
				{
					if (TmpRule.DescRuleId == psSoughtId)
						return TmpRule;
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
				}

				foreach (var TmpRule in TmpChildSet.AssertiveRules)
				{
					if (TmpRule.DescRuleId == psSoughtId)
						return TmpRule;
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
			else if (targetRule is CustomOperatorRule)
			{
				RuleDesc.Append(" (CustomOperatorRule -> " + targetRule.TargetAttribute.AttrName);
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

				if (CustomOpRule.CustomOpDelegate == BlazorAppNethereumExtensions.CheckBalanceIsWithinRange)
				{
					var CustomOpParams = CustomOpRule.DomainValueProps.Keys.ToList();

					RuleDesc.Append("Target: " + CustomOpRule.TargetAttribute.AttrName +
									"||Operator: " + CustomOpRule.CustomOpName +
									"||Param - EOA Address(" + CustomOpParams[0] + ")" +
									"||Param - Min Value(" + CustomOpParams[1] + ")" +
									"||Param - Max Value(" + CustomOpParams[2] + ")");
				}
				else
				{
					RuleDesc.Append("Target: " + CustomOpRule.TargetAttribute.AttrName +
									"||Operator: " + CustomOpRule.CustomOpName +
									"||Parameters(" + String.Join(",", CustomOpRule.DomainValueProps.Keys.ToList()) + ")");
				}
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

		public static void ImportMetadata(this WonkaRefEnvironment poRefEnv, string psMetadataFileContents)
		{
			XmlTextReader MetadataXmlReader = new XmlTextReader(new StringReader(psMetadataFileContents));
			MetadataXmlReader.XmlResolver   = null;
			MetadataXmlReader.DtdProcessing = DtdProcessing.Ignore;
			MetadataXmlReader.Namespaces    = false;

			XmlDocument XMLDoc = new XmlDocument();
			XMLDoc.Load(MetadataXmlReader);

			var GroupRedirectMap = new Dictionary<int, int>();
			var CadreRedirectMap = new Dictionary<int, int>();

			XmlNodeList GroupList = XMLDoc.GetElementsByTagName("Group");
			if ((GroupList != null) && (GroupList.Count > 0))
			{
				foreach (XmlNode TempGroupNode in GroupList)
				{
					var TempGroup =
						new XmlSerializer(typeof(WonkaRefGroup), new XmlRootAttribute("Group"))
						.Deserialize(new StringReader(TempGroupNode.OuterXml)) as WonkaRefGroup;

					if (!poRefEnv.GroupCache.Any(x => x.GroupName == TempGroup.GroupName))
					{
						int nGroupId = TempGroup.GroupId;

						if (poRefEnv.GroupCache.Any(x => x.GroupId == TempGroup.GroupId))
							TempGroup.GroupId = mnGroupCounter++;

						GroupRedirectMap[nGroupId] = TempGroup.GroupId;

						poRefEnv.GroupCache.Add(TempGroup);
					}
				}
			}

			XmlNodeList CadreList = XMLDoc.GetElementsByTagName("Cadre");
			if ((CadreList != null) && (CadreList.Count > 0))
			{
				foreach (XmlNode TempCadreNode in CadreList)
				{
					var TempCadre =
						new XmlSerializer(typeof(WonkaRefCadre), new XmlRootAttribute("Cadre"))
						.Deserialize(new StringReader(TempCadreNode.OuterXml)) as WonkaRefCadre;

					if (!poRefEnv.CadreCache.Any(x => x.CadreName == TempCadre.CadreName))
					{
						int nCadreId = TempCadre.CadreId;

						if (poRefEnv.CadreCache.Any(x => x.CadreId == TempCadre.CadreId))
							TempCadre.CadreId = mnFieldCounter++;

						CadreRedirectMap[nCadreId] = TempCadre.CadreId;

						poRefEnv.CadreCache.Add(TempCadre);
					}
				}
			}

			XmlNodeList AttrList = XMLDoc.GetElementsByTagName("Attr");
			if ((AttrList != null) && (AttrList.Count > 0))
			{
				foreach (XmlNode TempAttrNode in AttrList)
				{
					var TempAttr =
				        new XmlSerializer(typeof(WonkaRefAttr), new XmlRootAttribute("Attr"))
						.Deserialize(new StringReader(TempAttrNode.OuterXml)) as WonkaRefAttr;

					if (!poRefEnv.AttrCache.Any(x => x.AttrName == TempAttr.AttrName))
					{
						if (poRefEnv.AttrCache.Any(x => x.AttrId == TempAttr.AttrId))
							TempAttr.AttrId = mnAttrCounter++;

						TempAttr.FieldId = CadreRedirectMap[TempAttr.FieldId];
						TempAttr.GroupId = GroupRedirectMap[TempAttr.GroupId];

						poRefEnv.AttrCache.Add(TempAttr);
					}
				}
			}

		}

		public static bool IsValidRuleSet(this WonkaBizRuleSet poTargetRuleSet)
		{
			return (poTargetRuleSet.ChildRuleSets.Count > 0) || (poTargetRuleSet.AssertiveRules.Count > 0) || (poTargetRuleSet.EvaluativeRules.Count > 0);
		}

		public static ContractConfig ReadConfigXml(string psConfigXml)
		{
			XmlTextReader ConfigXmlReader = new XmlTextReader(new StringReader(psConfigXml));
			ConfigXmlReader.XmlResolver   = null;
			ConfigXmlReader.DtdProcessing = DtdProcessing.Ignore;
			ConfigXmlReader.Namespaces    = false;

			return new XmlSerializer(typeof(ContractConfig), new XmlRootAttribute("ContractConfig")).Deserialize(ConfigXmlReader) as ContractConfig;
		}

        public static void RemoveRuleById(this WonkaBizRuleSet poTargetSet, string psSoughtId)
		{
			poTargetSet.EvaluativeRules.RemoveAll(x => x.DescRuleId == psSoughtId);

			poTargetSet.AssertiveRules.RemoveAll(x => x.DescRuleId == psSoughtId);
		}

		public static string ToXml(this WonkaBizRulesEngine poEngine)
		{
			var RulesWriter =
				new Wonka.BizRulesEngine.Writers.WonkaBizRulesXmlWriter(poEngine);

			return RulesWriter.ExportXmlString();
		}
	}
}