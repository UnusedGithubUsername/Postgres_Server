using System; 
using System.Net.Sockets;
using System.Security.Cryptography; 
using System.IO;
using static Server.Enums;
using System.Text;
using System.Collections.Generic;  
using System.Threading.Tasks; 
using System.Net.NetworkInformation;
using System.Net.Http;

namespace Server {



    public struct SkillFile {
        public SkillID ID = SkillID.None;
        public string Name = "skill1";
        public string Description = "descriptionxxx"; 

        public SkillFile(SkillID id, string name, string skillDescription) {
            ID = id; 
            Name = name;
            Description = skillDescription; 
        }

        public byte[] ToByte() {
            char[] nameArray = Name.ToCharArray();
            char[] descArray = Description.ToCharArray();

            byte[] bytes = new byte[(nameArray.Length + descArray.Length + 2) * 2 + 1];
            byte[] shortByteArray = BitConverter.GetBytes((short)ID);
            bytes[0] = (byte)ID;
            Buffer.BlockCopy(shortByteArray, 0, bytes, 0, 2);
            bytes[2] = (byte)nameArray.Length;
            Buffer.BlockCopy(nameArray, 0, bytes, 3, nameArray.Length*2);

            bytes[(nameArray.Length * 2) + 3] = (byte)descArray.Length;
            Buffer.BlockCopy(descArray, 0, bytes, (nameArray.Length * 2) + 4, descArray.Length * 2);
            return bytes;
        }

        public SkillFile(string path) {
            File.ReadAllBytes(path);
        }
    }

    public struct CustomizedCharacter {
        //controlling data
        public int characterType = -1;
        public int Guid = -1;
        public int Level = -1;

        //actual gameplay data
        public string Name = "test";
        public string UniqueAbilityName = "testDescName";
        public string UniqueAbility = "testDesc";
        public byte[] stats = new byte[4];
        public byte[] statsPerLevel = new byte[4];
        public byte[] skills = new byte[10];
        public byte statpointsFullyAllocated = 0; 
        public byte skillpointsFullyAllocated = 0;
        public CustomizedCharacter(byte[] characterData) {
            Buffer.BlockCopy(characterData, 0, stats, 0, stats.Length );
            Buffer.BlockCopy(characterData, 4, statsPerLevel, 0, statsPerLevel.Length );
            Buffer.BlockCopy(characterData, 8, skills, 0, skills.Length );
            statpointsFullyAllocated = characterData[18];
            skillpointsFullyAllocated = characterData[19];
            byte nameLen = characterData[20];
            //char[] nameArray = new char[nameLen];
            //Buffer.BlockCopy(characterData, 21, nameArray, 0, nameLen*2);
            //Name = nameArray.ToString();
            Name = Encoding.UTF8.GetString(characterData, 21, nameLen);

        }

        //generated from staticData i.e. baseValues
        public CustomizedCharacter(byte[] bStats, byte[] sPLevel, byte[] sklz, string name, string uniqueAbilityName, string abilityDescription) { 
            Buffer.BlockCopy(bStats,    0, stats,       0, stats.Length );
            Buffer.BlockCopy(sPLevel,   0, statsPerLevel,   0, statsPerLevel.Length );
            Buffer.BlockCopy(sklz,      0, skills,          0, skills.Length );
            statpointsFullyAllocated = 0;
            skillpointsFullyAllocated = 0;
            Name = name;
            UniqueAbilityName = uniqueAbilityName;
            UniqueAbility = abilityDescription;
        }

        public void SetIDs(int CharacterType, int charGuid, int level) {
            characterType = CharacterType;
            Guid = charGuid;
            Level = level;

        }

        public byte[] ToByte() {
            byte[] nameArray = Encoding.UTF8.GetBytes(Name);

            byte[] dataAsByte = new byte[21 + nameArray.Length];

            Buffer.BlockCopy(stats,     0,      dataAsByte, 0, 4);
            Buffer.BlockCopy(statsPerLevel, 0,  dataAsByte, 4, 4);
            Buffer.BlockCopy(skills,        0,  dataAsByte, 8, 10);
            dataAsByte[18] = statpointsFullyAllocated;
            dataAsByte[19] = skillpointsFullyAllocated;
            dataAsByte[20] = (byte)nameArray.Length;

            Buffer.BlockCopy(nameArray, 0, dataAsByte, 21, nameArray.Length);
            return dataAsByte; 
        }
    }



    public struct StreamResult {
        public byte[] data;
        public int dataIndex = 0; 
        public StreamResult(ref Socket connection, ref RSACryptoServiceProvider rsa) {
            //in order to have secure communication for passwords etc, we need encryption. But its to cumbersome to encrypt everything, so only 
            //the important stuff is encrypted. encryption expands the size to 128 bytes even though the source data is only 52 bytes in the case of login
            //thats why several blockcopies are needed

            data = new byte[connection.Available];//create new array
            connection.Receive(data); //Read all data. the first 128 bytes are encrypted. the decrypted 128bytes are usually far fewer bytes
            //either the client token is encrypted into 128 bytes, or the full login package. either way, trailing bytes are unencrypted 

            //decrypt the first 128 bytes and write them to data[]
            if (data.Length >= 132) {

                byte[] dataToDecrypt = new byte[128];
                Buffer.BlockCopy(data, 4, dataToDecrypt, 0, 128);
                try {
                    dataToDecrypt = rsa.Decrypt(dataToDecrypt, false);
                    
                }
                catch (Exception) {
                    return;
                }
                byte[] decryptedFullData = new byte[dataToDecrypt.Length + data.Length - 128];
                Buffer.BlockCopy(dataToDecrypt, 0, decryptedFullData, 4, dataToDecrypt.Length);
                Buffer.BlockCopy(data, 0, decryptedFullData, 0, 4);
                Buffer.BlockCopy(data, 132, decryptedFullData, dataToDecrypt.Length + 4, data.Length - 132);
                data = decryptedFullData;

            }

        } 

        public void ConvertForewardedLoginPackage(ref byte[] loginPackage, ref RSACryptoServiceProvider rsa) {
            byte[] dataToDecrypt = new byte[128];
            Buffer.BlockCopy(loginPackage, 4, dataToDecrypt, 0, 128);
            try {
                dataToDecrypt = rsa.Decrypt(dataToDecrypt, false);
            }
            catch (Exception) {
                loginPackage = new byte[200]; //empty array
            }

            byte[] decryptedFullData = new byte[dataToDecrypt.Length + loginPackage.Length - 128];
            Buffer.BlockCopy(dataToDecrypt, 0, decryptedFullData, 4, dataToDecrypt.Length);
            Buffer.BlockCopy(loginPackage, 0, decryptedFullData, 0, 4);
            Buffer.BlockCopy(loginPackage, 132, decryptedFullData, dataToDecrypt.Length + 4, loginPackage.Length - 132);
            data = decryptedFullData;
            dataIndex = 8; //packageType and token are not relevatn
        }

        public string ReadString() {
            try {
                int stringLength = this.ReadInt();
                string result = System.Text.Encoding.UTF8.GetString(data, dataIndex, stringLength);
                dataIndex += stringLength;

                return result;
            }
            catch {
                return "";
            } 
        }
          
        public byte[] ReadBytes() {
            int stringLength = this.ReadInt();
            byte[] stringArray = new byte[stringLength];//create new array, 1 byte per char  
            Buffer.BlockCopy(data, dataIndex, stringArray, 0, stringLength);
            dataIndex += stringLength;

            return stringArray;
        }

        public int ReadInt() {
            if (dataIndex >= data.Length)
                return 0;

            int intRead = BitConverter.ToInt32(data, dataIndex);
            dataIndex += 4;
            return intRead;
        }
          
        public int BytesLeft() {
            return data.Length - dataIndex;
        }
    }

    public static class ServerHelper {
        public const string BasePath = "C:\\Users\\Klauke\\Documents\\My Games\\Corivi\\LauncherServer\\";

        public static void EnsureFolderExists(string folderName) {
            if (!Directory.Exists(folderName)) 
                Directory.CreateDirectory(folderName); 
        }

        public static byte[] Pack(List<byte[]> data) { // convert a list of byte[] into a single byte[]
            int size = 0;//count the total length
            for (int i = 0; i < data.Count; i++) 
                size += data[i].Length;

            byte[] packed= new byte[size];

            int offset = 0;
            for (int i = 0; i < data.Count; i++) { //copy everything over
                Buffer.BlockCopy(data[i], 0, packed, offset, data[i].Length);
                offset += data[i].Length;
            }

            return packed;
            
        }


        public static async Task<string> GetLocalIPv4(NetworkInterfaceType _type) {
            using (HttpClient client = new HttpClient()) {
                HttpResponseMessage response = await client.GetAsync("https://api.ipify.org");
                response.EnsureSuccessStatusCode();
                string publicIpAddress = await response.Content.ReadAsStringAsync();
                return publicIpAddress;
            }
        }

        public static bool Send(ref Socket s, PacketTypeServer type, byte[] data) //Send the data and append the data length and the packet type
        {
            byte[] packetLength = BitConverter.GetBytes(data.Length + 4);
            byte[] packetType = BitConverter.GetBytes((int)type);
            byte[] fullData = CombineBytes(packetLength, packetType, data);

            try {
                s.Send(fullData, 0, fullData.Length, SocketFlags.None);
            }
            catch (Exception) {
                return false;
            }
            return true;
        }

        public static byte[] CombineBytes(byte[] first, byte[] second) {
            byte[] bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }

        public static byte[] CombineBytes(byte[] first, byte[] second, byte[] third) {
            byte[] bytes = new byte[first.Length + second.Length + third.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            Buffer.BlockCopy(third, 0, bytes, first.Length + second.Length, third.Length);
            return bytes;
        }

        public static string GenerateActivationKey(int length) {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder result = new StringBuilder(length);
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) {
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0) {  
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    result.Append(validChars[(int)(num % (uint)validChars.Length)]);
                }
            }

            return result.ToString();
        }
    }
}
