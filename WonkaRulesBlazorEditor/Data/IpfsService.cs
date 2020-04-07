using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Ipfs;
using Ipfs.Http;

namespace WonkaRulesBlazorEditor.Data
{
    public class IpfsService
    {
        public async Task<Cid> AddFileAsync(string psIpfsUrl, FileInfo poFileInfo)
        {
            var ipfs = new IpfsClient(psIpfsUrl);

            var systemNode = await ipfs.FileSystem.AddFileAsync(poFileInfo.FullName).ConfigureAwait(false);
            await ipfs.Pin.AddAsync(systemNode.Id);

            return systemNode.Id;
        }

        public async Task<Cid> AddTextAsync(string psIpfsUrl, string psText, string psName, bool pbPinFlag = true)
        {
            var ipfs = new IpfsClient(psIpfsUrl);

            var systemNode = ipfs.FileSystem.AddTextAsync(psText).Result;
            await ipfs.Pin.AddAsync(systemNode.Id);

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

            return systemNode.Id;
        }

    }
}
