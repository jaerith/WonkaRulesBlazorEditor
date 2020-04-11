using System;

namespace WonkaRulesBlazorEditor.Data
{
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ContractConfig
    {
        public ContractConfig()
        {
            hostUrl = contractAbi = contractAddress = contractName = "";
        }

        private string hostUrl;
        private string contractAbi;
        private string contractAddress;
        private string contractName;
        
        public string HostUrl
        {
            get
            {
                return this.hostUrl;
            }
            set
            {
                this.hostUrl = value;
            }
        }

        public string ContractABI
        {
            get
            {
                return this.contractAbi;
            }
            set
            {
                this.contractAbi = value;
            }
        }

        public string ContractAddress
        {
            get
            {
                return this.contractAddress;
            }
            set
            {
                this.contractAddress = value;
            }
        }

        public string ContractName
        {
            get
            {
                return this.contractName;
            }
            set
            {
                this.contractName = value;
            }
        }

    }
}
