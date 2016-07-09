using System;
using System.IO;
using System.Linq;
using System.Text;
using IEC60870.Connections;
using IEC60870.Enum;

namespace IEC60870.Object
{
    /// <summary>
    /// The application service data unit (ASDU).
    /// </summary>
    /// <remarks>
    /// The ASDU is the payload of the application protocol data unit (APDU).
    /// Its structure is definied in IEC 60870-5-101.
    /// The ASDU consists of the Data Unit Identifier and a number of Information Objects.
    /// <list type="bullet">
    /// <listheader><term>The Data Unit Identifier contains:</term></listheader>
    /// <item><see cref="TypeId"/>(1 byte)</item>
    /// <item>Variable Structure Qualifier (1 byte) - specifies how many Information Obbjects and Information Element sets are part of the ASDU</item>
    /// <item>Cause of Transmission (COT, 1 or 2 bytes) - The first byte codes the actual <see cref="CauseOfTransmission"/>, a bit indicating whether the message was sent for test purposes only and a bit indicating whether a confirmation message is positive or negative. The optional second byte of the Cause of Transmission field is the Originator Address. It is the address of the originating controlling station so that responses can be routed back to it.</item>
    /// <item>Common Address of ASDU (1 or 2 bytes) - The address of the target station or the broadcast address. If the field length of the common address is 1 byte then the addresses 1 to 254 are used to address a particular station (station address) and 255 is used for broadcast addressing. If the field length of the common address is 2 bytes then the addresses 1 to 65534 are used to address a particular station and 65535 is used for broadcast addressing. Broadcast addressing is only allowed for certain TypeIDs</item>
    /// <item>A list of <see cref="InformationObject"/> Objects containing the actual data in the form of <see cref="IE.Base.InformationElement"/> Elements</item>
    /// </list>
    /// </remarks>
    public class ASdu
    {
        private readonly CauseOfTransmission causeOfTransmission;
        private readonly int commonAddress;
        private readonly InformationObject[] informationObjects;
        private readonly bool negativeConfirm;
        private readonly int originatorAddress;
        private readonly byte[] privateInformation;
        private readonly int sequenceLength;
        private readonly bool test;
        private readonly TypeId typeId;

        public bool IsSequenceOfElements;

        /// <summary>
        /// Use this constructor to create standardized ASDUs.
        /// </summary>
        /// <param name="typeId">type identification field that defines the purpose and contents of the ASDU</param>
        /// <param name="isSequenceOfElements">if false then the ASDU contains a sequence of information objects consisting of a fixed number of information elements. If true the ASDU contains a single information object with a sequence of elements.</param>
        /// <param name="causeOfTransmission">the cause of transmission</param>
        /// <param name="test">true if the ASDU is sent for test purposes</param>
        /// <param name="negativeConfirm">true if the ASDU is a negative confirmation</param>
        /// <param name="originatorAddress">the address of the originating controlling station so that responses can be routed back to it</param>
        /// <param name="commonAddress">the address of the target station or the broadcast address.</param>
        /// <param name="informationObjects">the information objects containing the actual data</param>
        public ASdu(TypeId typeId, bool isSequenceOfElements, CauseOfTransmission causeOfTransmission, bool test,
            bool negativeConfirm, int originatorAddress, int commonAddress, InformationObject[] informationObjects)
        {
            IsSequenceOfElements = isSequenceOfElements;

            this.typeId = typeId;
            this.causeOfTransmission = causeOfTransmission;
            this.test = test;
            this.negativeConfirm = negativeConfirm;
            this.originatorAddress = originatorAddress;
            this.commonAddress = commonAddress;
            this.informationObjects = informationObjects;

            privateInformation = null;

            sequenceLength = isSequenceOfElements
                ? informationObjects[0].GetInformationElements().Length
                : informationObjects.Length;
        }

        /// <summary>
        /// Use this constrcutor to create private ASDU with typeIDs in the range of 128-255.
        /// </summary>
        /// <param name="typeId">type identification field that defines the purpose and contents of the ASDU</param>
        /// <param name="isSequenceOfElements">if false then the ASDU contains a sequence of information objects consisting of a fixed number of information elements. If true the ASDU contains a single information object with a sequence of elements.</param>
        /// <param name="sequenceLength">the number of information objects or the number elements depending depending on which is transmitted as a sequence</param>
        /// <param name="causeOfTransmission">the cause of transmission</param>
        /// <param name="test">true if the ASDU is sent for test purposes</param>
        /// <param name="negativeConfirm">true if the ASDU is a negative confirmation</param>
        /// <param name="originatorAddress">the address of the originating controlling station so that responses can be routed back to it</param>
        /// <param name="commonAddress">the address of the target station or the broadcast address.</param>
        /// <param name="privateInformation">the bytes to be transmitted as payload</param>
        public ASdu(TypeId typeId, bool isSequenceOfElements, int sequenceLength,
            CauseOfTransmission causeOfTransmission, bool test, bool negativeConfirm, int originatorAddress,
            int commonAddress, byte[] privateInformation)
        {
            this.typeId = typeId;
            IsSequenceOfElements = isSequenceOfElements;
            this.causeOfTransmission = causeOfTransmission;
            this.test = test;
            this.negativeConfirm = negativeConfirm;
            this.originatorAddress = originatorAddress;
            this.commonAddress = commonAddress;
            informationObjects = null;
            this.privateInformation = privateInformation;
            this.sequenceLength = sequenceLength;
        }

        public ASdu(BinaryReader reader, ConnectionSettings settings, int aSduLength)
        {
            int typeIdCode = reader.ReadByte();

            typeId = (TypeId) typeIdCode;

            int tempbyte = reader.ReadByte();

            IsSequenceOfElements = (tempbyte & 0x80) == 0x80;

            int numberOfSequenceElements;
            int numberOfInformationObjects;

            sequenceLength = tempbyte & 0x7f;
            if (IsSequenceOfElements)
            {
                numberOfSequenceElements = sequenceLength;
                numberOfInformationObjects = 1;
            }
            else
            {
                numberOfInformationObjects = sequenceLength;
                numberOfSequenceElements = 1;
            }

            tempbyte = reader.ReadByte();
            causeOfTransmission = (CauseOfTransmission) (tempbyte & 0x3f);
            test = (tempbyte & 0x80) == 0x80;
            negativeConfirm = (tempbyte & 0x40) == 0x40;

            if (settings.CotFieldLength == 2)
            {
                originatorAddress = reader.ReadByte();
                aSduLength--;
            }
            else
            {
                originatorAddress = -1;
            }

            if (settings.CommonAddressFieldLength == 1)
            {
                commonAddress = reader.ReadByte();
            }
            else
            {
                commonAddress = reader.ReadByte() + (reader.ReadByte() << 8);
                aSduLength--;
            }

            if (typeIdCode < 128)
            {
                informationObjects = new InformationObject[numberOfInformationObjects];

                for (var i = 0; i < numberOfInformationObjects; i++)
                {
                    informationObjects[i] = new InformationObject(reader, typeId, numberOfSequenceElements, settings);
                }

                privateInformation = null;
            }
            else
            {
                informationObjects = null;
                privateInformation = reader.ReadBytes(aSduLength - 4);
            }
        }

        public TypeId GetTypeIdentification()
        {
            return typeId;
        }

        public int GetSequenceLength()
        {
            return sequenceLength;
        }

        public CauseOfTransmission GetCauseOfTransmission()
        {
            return causeOfTransmission;
        }

        public bool IsTestFrame()
        {
            return test;
        }

        public bool IsNegativeConfirm()
        {
            return negativeConfirm;
        }

        public int GetOriginatorAddress()
        {
            return originatorAddress;
        }

        public int GetCommonAddress()
        {
            return commonAddress;
        }

        public InformationObject[] GetInformationObjects()
        {
            return informationObjects;
        }

        public byte[] GetPrivateInformation()
        {
            return privateInformation;
        }

        public int Encode(byte[] buffer, int i, ConnectionSettings settings)
        {
            var origi = i;

            buffer[i++] = (byte) typeId;
            if (IsSequenceOfElements)
            {
                buffer[i++] = (byte) (sequenceLength | 0x80);
            }
            else
            {
                buffer[i++] = (byte) sequenceLength;
            }

            if (test)
            {
                if (negativeConfirm)
                {
                    buffer[i++] = (byte) ((byte) causeOfTransmission | 0xC0);
                }
                else
                {
                    buffer[i++] = (byte) ((byte) causeOfTransmission | 0x80);
                }
            }
            else
            {
                if (negativeConfirm)
                {
                    buffer[i++] = (byte) ((byte) causeOfTransmission | 0x40);
                }
                else
                {
                    buffer[i++] = (byte) causeOfTransmission;
                }
            }

            if (settings.CotFieldLength == 2)
            {
                buffer[i++] = (byte) originatorAddress;
            }

            buffer[i++] = (byte) commonAddress;

            if (settings.CommonAddressFieldLength == 2)
            {
                buffer[i++] = (byte) (commonAddress >> 8);
            }

            if (informationObjects != null)
            {
                i = informationObjects.Aggregate(i,
                    (current, informationObject) => current + informationObject.Encode(buffer, current, settings));
            }
            else
            {
                Array.Copy(privateInformation, 0, buffer, i, privateInformation.Length);
                i += privateInformation.Length;
            }

            return i - origi + 1;
        }

        public override string ToString()
        {
            var builder = new StringBuilder("Type ID: " + (int) typeId + ", " + Description.GetAttr(typeId).Name +
                                            "\nCause of transmission: " + causeOfTransmission + ", test: "
                                            + IsTestFrame() + ", negative con: " + IsNegativeConfirm() +
                                            "\nOriginator address: "
                                            + originatorAddress + ", Common address: " + commonAddress);

            if (informationObjects != null)
            {
                foreach (var informationObject in informationObjects)
                {
                    builder.Append("\n");
                    builder.Append(informationObject);
                }
            }
            else
            {
                builder.Append("\nPrivate Information:\n");
                var l = 1;
                foreach (var b in privateInformation)
                {
                    if ((l != 1) && ((l - 1)%8 == 0))
                    {
                        builder.Append(' ');
                    }
                    if ((l != 1) && ((l - 1)%16 == 0))
                    {
                        builder.Append('\n');
                    }
                    l++;
                    builder.Append("0x");
                    var hexString = (b & 0xff).ToString("X");
                    if (hexString.Length == 1)
                    {
                        builder.Append(0);
                    }
                    builder.Append(hexString + " ");
                }
            }

            return builder.ToString();
        }
    }
}
