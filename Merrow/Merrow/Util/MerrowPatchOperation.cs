using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merrow.Util
{
    public class MerrowPatchOperationChain
    {
        private List<MerrowPatchOperation> operations = new List<MerrowPatchOperation>();

        public void ApplyOperations(IList<string> patchStrings)
        {
            for (int k=0; k<this.operations.Count; k++)
            {
                var operation = this.operations[k];
                operation.AddToPatchStrings(patchStrings);
            }
        }

        public void AddWriteOperation(MerrowPatchOperation operation)
        {
            this.operations.Add(operation);
        }

        public void AddWriteOperation(string romAddress, string patchContents)
        {
            var newOperation = new MerrowPatchOperation
            {
                romAddress = romAddress,
                patchContents = patchContents
            };

            this.operations.Add(newOperation);
        }
    }

    public class MerrowPatchOperation
    {
        public string romAddress;
        public string patchContents;

        public string GetMerrowROMAddress()
        {
            return this.romAddress;
        }

        public void SetRomAddress(string romAddress)
        {
            this.romAddress = romAddress;
        }

        public void SetPatchContents(string patchContents)
        {
            this.patchContents = patchContents;
        }

        public void AddToPatchStrings(IList<string> patchStrings)
        {
            var writeLength = this.patchContents.Length / 2;

            patchStrings.Add(this.romAddress);
            patchStrings.Add(writeLength.ToString("X4"));
            patchStrings.Add(this.patchContents);
        }
    }
}
