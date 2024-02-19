using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LiveSplit.UI.Components
{
    internal class Split
    {
        public string name { get; set; }
        public string address { get; set; }
        public string value { get; set; }
        public string type { get; set; }
        public List<Split> more { get; set; }
        public List<Split> next { get; set; }

        [JsonIgnore]
        public int posToCheck { get; set; } = 0;
        [JsonIgnore]
        public uint addressInt { get; set; } = 0;
        [JsonIgnore]
        public uint valueInt { get; set; } = 0;
        [JsonIgnore]
        public Func<byte[], bool> checkValue { get; set; }
        [JsonIgnore]
        public List<Tuple<uint, uint>> addressSizePairs { get; set; }

        public bool check(List<byte[]> bytes)
        {
            if (bytes == null || bytes.Count != addressSizePairs.Count)
            {
                throw new Exception("Incorrect number of bytes for split");
            }

            if (!checkValue(bytes[0]))
            {
                return false;
            }

            if (more == null)
            {
                return true;
            }

            int dataIndex = 1;
            foreach (var moreSplit in more)
            {
                if (!moreSplit.checkValue(bytes[dataIndex]))
                    return false;
                dataIndex++;
            }

            return true;
        }

        public void validate()
        {
            addressInt = Convert.ToUInt32(address, 16) & 0xFFFF;
            valueInt = Convert.ToUInt32(value, 16);
            addressSizePairs = new List<Tuple<uint, uint>> { new Tuple<uint, uint>(addressInt, 2) };

            if (more != null)
            {
                foreach (var moreSplit in more)
                {
                    if (moreSplit.more != null)
                    {
                        throw new NotSupportedException("Nested 'more' splits are not supported");
                    }
                    if (moreSplit.next != null)
                    {
                        throw new NotSupportedException("Nested 'next' splits are not supported");
                    }
                    moreSplit.validate();
                    addressSizePairs.AddRange(moreSplit.addressSizePairs);
                }
            }
            if (next != null)
            {
                foreach (var nextSplit in next)
                {
                    if (nextSplit.next != null)
                    {
                        throw new NotSupportedException("Nested 'next' splits are not supported");
                    }
                    nextSplit.validate();
                }
            }

            string operation;
            Func<byte[], uint> data;
            if (!type.StartsWith("w"))
            {
                data = (bytes) => bytes[0];
                operation = type;
                valueInt &= 0xFF;
            }
            else
            {
                data = (bytes) => (uint)((bytes[1] << 8) | bytes[0]);
                operation = type.Substring(1);
                valueInt &= 0xFFFF;
            }

            switch (operation)
            {
                case "bit":
                    checkValue = (bytes) => (data(bytes) & this.valueInt) != 0;
                    break;
                case "eq":
                    checkValue = (bytes) => data(bytes) == this.valueInt;
                    break;
                case "gt":
                    checkValue = (bytes) => data(bytes) > this.valueInt;
                    break;
                case "lt":
                    checkValue = (bytes) => data(bytes) < this.valueInt;
                    break;
                case "gte":
                    checkValue = (bytes) => data(bytes) >= this.valueInt;
                    break;
                case "lte":
                    checkValue = (bytes) => data(bytes) <= this.valueInt;
                    break;
                default:
                    throw new NotSupportedException($"Unknown type: {type}");
            }
        }
    }
}
