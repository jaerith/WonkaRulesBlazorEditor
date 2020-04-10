using System;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.ENS;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

using Wonka.BizRulesEngine;
using Wonka.BizRulesEngine.Reporting;

using WonkaRulesBlazorEditor.Data;

namespace WonkaRulesBlazorEditor.Extensions
{
	[Function("balanceOf", "uint256")]
	public class BalanceOfFunction : FunctionMessage
	{
		[Parameter("address", "_owner", 1)] public string Owner { get; set; }
	}

	[Event("Transfer")]
	public class TransferEventDTO : IEventDTO
	{
		[Parameter("address", "_from", 1, true)]
		public string From { get; set; }

		[Parameter("address", "_to", 2, true)]
		public string To { get; set; }

		[Parameter("uint256", "_value", 3, false)]
		public BigInteger Value { get; set; }
	}

	public static class BlazorAppNethereumExtensions
	{
		#region CONSTANTS

		public const string CONST_INFURA_IPFS_GATEWAY_URL     = "https://ipfs.infura.io/ipfs/";
		public const string CONST_INFURA_IPFS_API_GATEWAY_URL = "https://ipfs.infura.io:5001/7238211010344719ad14a89db874158c/api/";
		public const string CONST_TEST_INFURA_KEY             = "7238211010344719ad14a89db874158c";
		public const string CONST_TEST_INFURA_URL             = "https://mainnet.infura.io/v3/7238211010344719ad14a89db874158c";
		public const string CONST_ETH_FNDTN_EOA_ADDRESS       = "0xde0b295669a9fd93d5f28d9ec85e40f4cb697bae";
		public const string CONST_DAI_TOKEN_CTRCT_ADDRESS     = "0x89d24a6b4ccb1b6faa2625fe562bdd9a23260359";
		public const string CONST_MAKER_ERC20_CTRCT_ADDRESS   = "0x9f8f72aa9304c8b593d555f12ef6589cc3a579a2";
		public const string CONST_RULES_FILE_IPFS_KEY         = "QmXcsGDQthxbGW8C3Sx9r4tV9PGSj4MxJmtXF7dnXN5XUT";

		#endregion

		/*
		 * NOTE: This type of custom rule works to assign something to the target attribute, like normal custom operators -
		 *       however, it has the extra duty to raise a flag if no events are found, so it returns the audit review flag
		 *       in the case of success and a failure if empty
		 */
		public static string AnyEventsInBlockRange(string psContractAddress, string psMinBlockValue, string psMaxBlockValue, string psDummyValue = "")
		{
			// NOTE: Empty value indicates rule failure
			string sAuditReviewFlag = "";

			ulong nMinBlockNum = 0;
			ulong nMaxBlockNum = 0;

			var url  = CONST_TEST_INFURA_URL;
			var web3 = new Web3(url);

			// This is the contract address of the DAI Stablecoin v1.0 ERC20 token on mainnet
			// See https://etherscan.io/address/0x89d24a6b4ccb1b6faa2625fe562bdd9a23260359
			var erc20TokenContractAddress = psContractAddress;
			if (String.IsNullOrEmpty(psContractAddress))
				erc20TokenContractAddress = CONST_DAI_TOKEN_CTRCT_ADDRESS;

			var transferEventHandler = web3.Eth.GetEvent<TransferEventDTO>(erc20TokenContractAddress);

			if (!String.IsNullOrEmpty(psMinBlockValue))
				UInt64.TryParse(psMinBlockValue, out nMinBlockNum);

			if (!String.IsNullOrEmpty(psMaxBlockValue))
				UInt64.TryParse(psMaxBlockValue, out nMaxBlockNum);

			// Just examine a few blocks by specifying start and end blocks
			var filter
				= transferEventHandler.CreateFilterInput(fromBlock: new BlockParameter(nMinBlockNum), toBlock: new BlockParameter(nMaxBlockNum));

			var logs = transferEventHandler.GetAllChanges(filter).Result;

			/*
			Console.WriteLine($"Token Transfer Events for ERC20 Token at Contract Address: {erc20TokenContractAddress}");
			foreach (var logItem in logs)
				Console.WriteLine(
					$"tx:{logItem.Log.TransactionHash} from:{logItem.Event.From} to:{logItem.Event.To} value:{logItem.Event.Value}");
			 */

			if (logs.Count > 0)
				sAuditReviewFlag = "Y";

			return sAuditReviewFlag;
		}

		public static string CheckBalanceIsWithinRange(string psEOA, string psMinValue, string psMaxValue, string psDummyValue)
		{
			string sStatusCd = BlazorAppWonkaExtensions.CONST_STATUS_CD_INACTIVE;

			if (!String.IsNullOrEmpty(psEOA))
			{
				long nMinValue    = -1;
				long nMaxValue    = -1;
				long nTmpMinValue = 0;
				long nTmpMaxValue = 0;

				Int64.TryParse(psMinValue, out nTmpMinValue);
				Int64.TryParse(psMaxValue, out nTmpMaxValue);

				if (nTmpMinValue > 0)
					nMinValue = nTmpMinValue;

				if (nTmpMaxValue > 0)
					nMaxValue = nTmpMaxValue;

				var web3 = new Web3(CONST_TEST_INFURA_URL);

				// Check the balance of one of the accounts provisioned in our chain, to do that, 
				// we can execute the GetBalance request asynchronously:
				var balance  = web3.Eth.GetBalance.SendRequestAsync(psEOA).Result;
				var minValue = new BigInteger(nMinValue);
				var maxValue = new BigInteger(nMaxValue);

				if ((nMinValue > -1) && (nMaxValue > -1) && (BigInteger.Compare(balance, minValue) > 0) && (BigInteger.Compare(balance, maxValue) < 0))
					sStatusCd = BlazorAppWonkaExtensions.CONST_STATUS_CD_ACTIVE;
				else if ((nMinValue > -1) && (BigInteger.Compare(balance, minValue) > 0))
					sStatusCd = BlazorAppWonkaExtensions.CONST_STATUS_CD_ACTIVE;
				else if ((nMaxValue > -1) && (BigInteger.Compare(balance, maxValue) < 0))
					sStatusCd = BlazorAppWonkaExtensions.CONST_STATUS_CD_ACTIVE;
			}

			return sStatusCd;
		}

		public static string GetERC20Balance(string psContractAddress, string psOwner, string psDummyValue1 = "", string psDummyValue2 = "")
		{
			var url  = CONST_TEST_INFURA_URL;
			var web3 = new Web3(url);

			//Querying the Maker smart contract https://etherscan.io/address/0x9f8f72aa9304c8b593d555f12ef6589cc3a579a2
			var erc20TokenContractAddress = psContractAddress;
			if (String.IsNullOrEmpty(psContractAddress))
				erc20TokenContractAddress = CONST_MAKER_ERC20_CTRCT_ADDRESS;

			//Setting the owner https://etherscan.io/tokenholdings?a=0x8ee7d9235e01e6b42345120b5d270bdb763624c7
			if (String.IsNullOrEmpty(psOwner))
				psOwner = "0x8ee7d9235e01e6b42345120b5d270bdb763624c7";

			var balanceOfMessage = new BalanceOfFunction() { Owner = psOwner };

			//Creating a new query handler
			var queryHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();

			var balance =
				queryHandler.QueryAsync<BigInteger>(erc20TokenContractAddress, balanceOfMessage).Result;

			string sBalance = balance.ToString();

			return sBalance;
		}

		public static string GetContractSimpleMethodValue(string psContractABI, string psContractAddress, string psFunctionName, string psDummyValue = "")
		{
			var url = CONST_TEST_INFURA_URL;

			return new Web3(url).Eth.GetContract(psContractABI, psContractAddress).GetFunction(psFunctionName).CallAsync<string>().Result;
		}

		// NOTE: This code will need to be modified in order to work correctly
		public static async Task<string> RegisterOnENS(this WonkaBizRulesEngine poEngine)
		{
			var durationInDays = 365; // how long we are going to rent the domain for

			var ourName = "supersillynameformonkeys"; // Our xxxxx.eth domain name
			var owner   = "0x12890d2cce102216644c59dae5baed380d84830c"; // Owner of the domain
			var secret  = "animals in the forest";  //This is the commitment secret,

			// it should be unique and remember it to be able to finalise your registration
			var account = new Nethereum.Web3.Accounts.Account("YOURPRIVATE KEY");
			var web3    = new Web3(account, "https://mainnet.infura.io/v3/7238211010344719ad14a89db874158c");

			var ethTLSService = new EthTLSService(web3);
			await ethTLSService.InitialiseAsync(); // Initialising to retrieve the controller

			//How much is going to cost
			var price = await ethTLSService.CalculateRentPriceInEtherAsync(ourName, durationInDays);
			Console.WriteLine(price);

			//Calculate the commitment that will be submitted for reservation
			var commitment = await ethTLSService.CalculateCommitmentAsync(ourName, owner, secret);
			Console.WriteLine(commitment.ToHex());

			//You can now create your commitment and wait for it to be included on the chain
			var commitTransactionReceipt = await ethTLSService.CommitRequestAndWaitForReceiptAsync(commitment);

			// Once is on chain you can complete the registration 
			var txnHash = await ethTLSService.RegisterRequestAsync(ourName, owner, durationInDays, secret, price);

			return txnHash;
		}

		public static async Task<string> PublishReportToIpfs(this WonkaBizRuleTreeReport poReport,
																				  string psIpfsFilePath,
																			 IpfsService poIpfsService,
																					bool pbPinFlag = true,
																				  string psIpfsGateway = CONST_INFURA_IPFS_API_GATEWAY_URL)
		{
			string sIpfsHash = "";
			string sReport   = "";

			// NOTE: Not yet ready
			// sReport = poReport.SerializeToXml();
			sReport = poReport.GetErrors();

			if (String.IsNullOrEmpty(psIpfsFilePath))
				psIpfsFilePath = "testreport";

			if (String.IsNullOrEmpty(sReport))
				throw new Exception("ERROR!  No report to be serialized.");

			var IpfsNode =
				await poIpfsService.AddTextAsync(psIpfsGateway, sReport, psIpfsFilePath, pbPinFlag).ConfigureAwait(false);

			sIpfsHash = IpfsNode.Hash.ToString();

			return sIpfsHash;
		}

		public static async Task<string> PublishRulesToIpfs(this WonkaBizRulesEngine poEngine,
																			  string psIpfsFilePath,
																		 IpfsService poIpfsService,
																				bool pbPinFlag = true,
																			  string psIpfsGateway = CONST_INFURA_IPFS_API_GATEWAY_URL)
		{
			string sIpfsHash = "";
			string sRulesXml = "";

			sRulesXml = poEngine.ToXml();

			if (String.IsNullOrEmpty(psIpfsFilePath))
				psIpfsFilePath = "testruletree";

			if (String.IsNullOrEmpty(sRulesXml))
				throw new Exception("ERROR!  No rules XMl serialized from the rules engine.");

			var IpfsNode =
				await poIpfsService.AddTextAsync(psIpfsGateway, sRulesXml, psIpfsFilePath, pbPinFlag).ConfigureAwait(false);

			sIpfsHash = IpfsNode.Hash.ToString();

			return sIpfsHash;
		}
	}
}
