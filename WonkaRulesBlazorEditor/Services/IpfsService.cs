using System;
using System.IO;
using System.Threading.Tasks;

using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Http;

namespace WonkaRulesBlazorEditor.Services
{
    public class IpfsService
    {
        private readonly string ipfsUrl;

        public IpfsService(string ipfsUrl)
        {
            this.ipfsUrl = ipfsUrl;
        }

        public async Task<Cid> AddFileAsync(FileInfo fileInfo)
        {
            var ipfs = new IpfsClient(ipfsUrl);
            var systemNode = await ipfs.FileSystem.AddFileAsync(fileInfo.FullName).ConfigureAwait(false);
            await ipfs.Pin.AddAsync(systemNode.Id);
            return systemNode.Id;
        }

        public async Task<Cid> AddTextAsync(string psText)
        {
            var ipfs = new IpfsClient(ipfsUrl);
            var systemNode = await ipfs.FileSystem.AddTextAsync(psText).ConfigureAwait(false);
            await ipfs.Pin.AddAsync(systemNode.Id);
            return systemNode.Id;
        }
    }
}
