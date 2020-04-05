using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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

using WonkaRulesBlazorEditor.Services;

namespace WonkaRulesBlazorEditor.Extensions
{
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
		public const string CONST_RULES_FILE_IPFS_KEY         = "QmXcsGDQthxbGW8C3Sx9r4tV9PGSj4MxJmtXF7dnXN5XUT";

		#endregion

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
				= transferEventHandler.CreateFilterInput( fromBlock: new BlockParameter(nMinBlockNum), toBlock: new BlockParameter(nMaxBlockNum));

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
																	                bool pbPinFlag = true,
                                                                                  string psIpfsGateway = CONST_INFURA_IPFS_API_GATEWAY_URL)
		{
			string sIpfsHash = "";
			string sReport   = "";

			var ipfsSvc = new IpfsService(psIpfsGateway);

			sReport = poReport.GetErrors();

			// NOTE: Not yet ready
			// sReport = poReport.SerializeToXml();

			if (String.IsNullOrEmpty(psIpfsFilePath))
				psIpfsFilePath = "testreport";

			if (String.IsNullOrEmpty(sReport))
				throw new Exception("ERROR!  No report to be serialized.");

			var IpfsNode = await ipfsSvc.AddTextAsync(sReport).ConfigureAwait(false);

			sIpfsHash = IpfsNode.Hash.ToString();

			/*
			 * NOTE: Does not yet work due to hanging
			 * 
			using (MemoryStream RulesXmlInputStream = new MemoryStream(Encoding.UTF8.GetBytes(sReport)))
			{
				var IpfsFileNode =
					ipfsClient.FileSystem.AddAsync(RulesXmlInputStream, psIpfsFilePath, new AddFileOptions() { Pin = pbPinFlag }).Result;

				sIpfsHash = IpfsFileNode.Id.ToString();
			}
			*/

			return sIpfsHash;
		}

		public static async Task<string> PublishRulesToIpfs(this WonkaBizRulesEngine poEngine,
			                                                                  string psIpfsFilePath,
																                bool pbPinFlag = true,
																              string psIpfsGateway = CONST_INFURA_IPFS_API_GATEWAY_URL)
		{
			string sIpfsHash = "";
			string sRulesXml = "";

			var ipfsSvc = new IpfsService(psIpfsGateway);

			sRulesXml = poEngine.ToXml();

			if (String.IsNullOrEmpty(psIpfsFilePath))
				psIpfsFilePath = "testruletree";

			if (String.IsNullOrEmpty(sRulesXml))
				throw new Exception("ERROR!  No rules XMl serialized from the rules engine.");

			var IpfsNode = await ipfsSvc.AddTextAsync(sRulesXml).ConfigureAwait(false);

			sIpfsHash = IpfsNode.Hash.ToString();

			/*
			 * NOTE: Does not yet work due to hanging
			 * 
			var sIpfsFileNode =
				ipfsClient.FileSystem.AddFileAsync(psIpfsFilePath, new AddFileOptions() { Pin = pbPinFlag }).Result;

			using (MemoryStream RulesXmlInputStream = new MemoryStream(Encoding.UTF8.GetBytes(sRulesXml)))
			{
				var IpfsFileNode =
					ipfsClient.FileSystem.AddAsync(RulesXmlInputStream, psIpfsFilePath, new AddFileOptions() { Pin = pbPinFlag }).Result;

				sIpfsHash = IpfsFileNode.Id.ToString();
			}
			*/

			return sIpfsHash;
		}	}
}