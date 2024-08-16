using static Server.Enums; 
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows; 
using System.Net.Mail;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.Net.NetworkInformation;
using System.Linq;

namespace Server {

    public partial class MainWindow : Window {

        public const int MAX_FILE_BYTES = 65524; 
        public ObservableCollection<string> Chatoutput { get; set; }

        private const int port = 16501;
        private readonly TcpListener listener;
        private readonly List<Socket> connections;

        private RSACryptoServiceProvider rsa;
        private readonly string publicKey;
        private readonly string privateKey;
        private readonly Dictionary<int, int2> loginSessions = new();//lookup if a client is logged in, so he does not

        Socket gameServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        DateTime lastGameServerMessage = DateTime.Now;
        bool gameServerConnected = false;


        public DatabaseConnection DB;
        //have to send over username and password every time. Guid is the key, the session is the value
        public MainWindow() {
            Chatoutput = new (); 
            InitializeComponent();
            DB = new();
            rsa = new RSACryptoServiceProvider();
            publicKey = rsa.ToXmlString(false);
            privateKey = rsa.ToXmlString(true);
            rsa.FromXmlString(privateKey);

            connections = new List<Socket>(); 

            listener = new TcpListener(IPAddress.Any, port);//loopback means localhost
            listener.Start(10);

            Timer updateLoop = new(100);//create an update loop that runs 10 times per second to scan for connections and data
            updateLoop.Elapsed += Update;
            updateLoop.Enabled = true;
            updateLoop.AutoReset = true;
            updateLoop.Start();
            //SendEmail("dorian.klaucke@gmx.de", "doenerman", "1234567890");
            ReachGameServer(); 
        }


        private async void ReachGameServer() { //This connection has so many privileges, that incoming connections are never secure enough. This means this server will request them

            IPAddress ip = IPAddress.Parse("87.150.143.153");
            while (true) { //continously try to connect to Game. And check if connection is still active
                await Task.Delay(500);
                if (!gameServerConnected) {
                    try {
                        await gameServer.ConnectAsync(ip, 16515); 
                        SendPKeyPackage(gameServer);

                        lastGameServerMessage = DateTime.Now;
                        gameServerConnected = true;
                    }
                    catch { 
                        Chat("Server not Reachable");
                    }  
                }
                else { 
                    if ((DateTime.Now - lastGameServerMessage).TotalSeconds > 8.0f) {

                        gameServerConnected = false; 
                        gameServer.Close();
                        gameServer.Dispose();
                        gameServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
                    }
                }
            }
        }



        private void Update(object sender, ElapsedEventArgs e) {

            while (listener.Pending()) {
                 
                Socket connection = listener.AcceptSocket();//accept new connections ...
                connections.Add(connection);
                 
                Chat("Accepted a connection and SEND A Public KEY");
            }

            //all the communication to clients happens here
            for (int i = 0; i < connections.Count; i++)  
                if (connections[i].Available > 0)  
                    HandleConnectionRequest(connections[i]); 
             
            //The requests from the game are handled here
            if(gameServerConnected)
                if(gameServer.Available > 0)
                    HandleConnectionRequestGameserver(gameServer);

            //handle disconnect
            for (int i = 0; i < connections.Count; i++) {
                if (connections[i].Connected == false) {
                    connections.RemoveAt(i);
                    i--;
                }
            }
        }

        private Socket SendPKeyPackage(Socket connection) { 
            int byteLengthOfPublicKey = publicKey.Length;
            byte[] pKeyLen = BitConverter.GetBytes(byteLengthOfPublicKey);
            byte[] pKeyBytes = Encoding.UTF8.GetBytes(publicKey);
            byte[] intBytes = ServerHelper.CombineBytes(pKeyLen, pKeyBytes);
             
            ServerHelper.Send(ref connection, PacketTypeServer.publicKeyPackage, intBytes);
            return connection;
        }

        private async void HandleConnectionRequest(Socket connection) {
            StreamResult result = new(ref connection, ref rsa);

            if (HandleHTTP(result.data, connection)) {
                connection.Close();
                return;
            }

            PacketTypeClient type = (PacketTypeClient)result.ReadInt(); 

            if (PacketTypeClient.RequestKey == type) {
                SendPKeyPackage(connection);
                return;
            }

            int clientToken = result.ReadInt();

            if (type == PacketTypeClient.Login || type == PacketTypeClient.RegisterUser) {

                string email = Encoding.UTF8.GetString(result.ReadBytes());
                string UsernameR = Encoding.UTF8.GetString(result.ReadBytes());
                string encryptedPassword = result.ReadString();

                switch (type) {
                    case PacketTypeClient.RegisterUser:
                        //check if email/username combination exists, create account  
                        bool accountAvailable = DB.CheckIfAccountExists(UsernameR);
                        if (!accountAvailable) {
                            ServerHelper.Send(ref connection, PacketTypeServer.loginFailed, Array.Empty<byte>());
                            break;
                        }
                        string ActivationKey = ServerHelper.GenerateActivationKey(10);
                        DB.CreateAccount(email, encryptedPassword, UsernameR, ActivationKey);
                        SendEmail(email, UsernameR, ActivationKey);
                        break;

                    case PacketTypeClient.Login:
                        int netID = result.ReadInt();
                        Chat("A Client is trying to log in");
                        Login(email, encryptedPassword, connection, clientToken, netID);
                        break; 

                    default:
                        break;
                }
                return;
            }

            int userGUID = result.ReadInt();
            bool isValidLogin = (loginSessions[userGUID].sessionID == clientToken);//if the client has a verified active session
            if (!isValidLogin)
                return;

            switch (type) {  

                case PacketTypeClient.requestWithToken:
                    int requestType = result.ReadInt();
                    int characterGuid = result.ReadInt();
                     
                    int characterType = await DB.GetItemType(userGUID, characterGuid);
                    bool isValidCharacter = characterType > 1000;
                    if (!isValidCharacter)
                        break;

                    if (requestType == 0) { //save data
                        byte[] characterData = result.ReadBytes();
                        byte[] newCharacterData = await CharacterDB.SaveCharacterData(userGUID, characterGuid, characterData, this);
                        ServerHelper.Send(ref connection, PacketTypeServer.CharacterData, ServerHelper.CombineBytes(BitConverter.GetBytes(characterGuid), BitConverter.GetBytes(newCharacterData.Length), newCharacterData));

                    }
                    else if (requestType == 1) {//get a certain characters data  
                        byte[] characterData = CharacterDB.getCharacterData(userGUID, characterGuid, characterType).ToByte();
                        ServerHelper.Send(ref connection, PacketTypeServer.CharacterData, ServerHelper.CombineBytes(BitConverter.GetBytes(characterGuid), BitConverter.GetBytes(characterData.Length), characterData));
                    }
                    else if (requestType == 2) { //Levelup
                        int requestedLevel = result.ReadInt();
                        if (requestedLevel <= 10 && requestedLevel > 0) {
                            int remainingXP = await LevelupCharacter(requestedLevel, characterGuid, userGUID);
                            byte[] levelUpResult = ServerHelper.CombineBytes(BitConverter.GetBytes(remainingXP), BitConverter.GetBytes(characterGuid), BitConverter.GetBytes(requestedLevel));
                            ServerHelper.Send(ref connection, PacketTypeServer.levelupSuccessfull, levelUpResult);
                        }
                    }
                    else if (requestType == 3) {
                        byte[] characterData = CharacterDB.ResetStats(userGUID, characterGuid, characterType);
                        ServerHelper.Send(ref connection, PacketTypeServer.CharacterData, ServerHelper.CombineBytes(BitConverter.GetBytes(characterGuid), BitConverter.GetBytes(characterData.Length), characterData));

                    } 

                    break;

                case PacketTypeClient.Friendrequest:
                    FriendrequestAction reqType = (FriendrequestAction)result.ReadInt();
                    int friendGuid = result.ReadInt();

                    if (friendGuid < 0)//guids are larger than 0 so 0 is an invalid input
                        return;

                    HandleFriendaction(userGUID, reqType, friendGuid);
                    break;

                     
                case (PacketTypeClient.Message): 
                    int targetGUID = result.ReadInt();
                    string message = result.ReadString();
                     
                    Chat(message + "  by " + userGUID + "  to " + targetGUID);

                    if (!loginSessions.ContainsKey(targetGUID))
                        return;

                    Socket friendCon = loginSessions[targetGUID].connection;
                    byte[] msg = Encoding.UTF8.GetBytes(message);
                    if (!ServerHelper.Send(ref friendCon, PacketTypeServer.Message, ServerHelper.CombineBytes(BitConverter.GetBytes(userGUID), BitConverter.GetBytes(msg.Length), msg)))
                        loginSessions.Remove(targetGUID);

                    break;

                //if creation worked
                default:
                    break;
            }
        }

        private async void HandleConnectionRequestGameserver(Socket connection) {
            StreamResult result = new(ref connection, ref rsa);

            lastGameServerMessage = DateTime.Now;

            PacketTypeClient type = (PacketTypeClient)result.ReadInt();
            int clientToken = result.ReadInt();

            switch (type) {
                case PacketTypeClient.ForewardLoginPacket://normal client login.
                    Chat("recieved forewarded Login");
                    byte[] fwdLoginPacket = result.ReadBytes();
                    int actualNetID = result.ReadInt();
                    Chat(fwdLoginPacket.Length.ToString() + " bytes forewarded Login; netID=" + actualNetID.ToString());
                    result.ConvertForewardedLoginPackage(ref fwdLoginPacket, ref rsa);
                    string name = result.ReadString();
                    string encPW = result.ReadString();
                    int netID2 = result.ReadInt();
                    Chat(netID2.ToString() + " name= " + name + " pw=" + encPW);
                    Login(name, encPW, connection, clientToken, actualNetID);
                    break;

                case PacketTypeClient.Login: //this is only here for editor login
                    string email = Encoding.UTF8.GetString(result.ReadBytes());
                    string shouldBeEmptyUsername = Encoding.UTF8.GetString(result.ReadBytes());
                    string encryptedPassword = result.ReadString();
                    int netID = result.ReadInt();
                    Chat("A Client is trying to log in");
                    Login(email, encryptedPassword, connection, clientToken, netID);
                    break;
 

                //if creation worked
                default:
                    break;
            }
        }

        private void HandleFriendaction(int user, FriendrequestAction reqType, int friendGuid) {
            if (reqType == FriendrequestAction.RequestFriendship)
                DB.RequestFriendship(friendGuid, user);
            else if (reqType == FriendrequestAction.Accept) { 
                int affectedRows = DB.DenyFriendship(user, friendGuid);//deny just deletes the request and returns if it even existed
                if (affectedRows < 1)//if the request does not exists, it does could not have been accepted
                    return;

                SaveFriend(user, friendGuid);
                SaveFriend(friendGuid, user);
            }
            else if (reqType == FriendrequestAction.Deny) {
                DB.DenyFriendship(user, friendGuid);
            }
            else if (reqType == FriendrequestAction.Block){
                DB.BlockFriendship(user, friendGuid); 
            }
            else if (reqType == FriendrequestAction.Remove){

            }
        }

        //Endgültig löschen
        public async void UploadFile() {

            string aws_credentialsPath = @"C:\Users\Klauke\Documents\My Games\Corivi\AWS Key2.txt";//temp, but not as dumb as uploading that to github

            // Read all lines from the file
            string[] lines = File.ReadAllLines(aws_credentialsPath);
            string KeyID = lines[0];
            string AccessKey = lines[1];


            string folderPath = @"C:\Users\Klauke\Documents\My Games\Corivi\LauncherServer";



            //write the current IP adress there. This is also a kind of timestamp
            File.WriteAllText(folderPath + @"\ServerIP.txt", await ServerHelper.GetLocalIPv4(NetworkInterfaceType.Ethernet));

            //get all files in folder
            string bucketName = "corivi";
            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
            string[] relativeFilename = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                relativeFilename[i] = Path.GetRelativePath(folderPath, files[i]);//.Replace('\\', '\');


            AmazonS3Client s3Client = new AmazonS3Client(KeyID, AccessKey, RegionEndpoint.USEast2); 
            TransferUtility fileTransferUtility = new TransferUtility(s3Client);

            //qwe

            ListObjectsV2Request request = new ListObjectsV2Request {
                BucketName = bucketName
            };
            var objects2 = new Dictionary<string, S3Object>();
            ListObjectsV2Response response;
            do {
                response = await s3Client.ListObjectsV2Async(request);

                foreach (S3Object entry in response.S3Objects) {
                    objects2.Add(entry.Key, entry);
                }

                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated); 
              
            S3Object timerObject;
            if (!objects2.TryGetValue(@"ServerIP.txt", out timerObject))
                return;

            DateTime time = timerObject.LastModified;

            bool[] uploadFile = new bool[files.Length];
            for (int i = 0; i < files.Length; i++)  
                uploadFile[i] = File.GetLastWriteTime(files[i]) > time; //only upload file if it was modified recently  ....
             
            for (int i = 0; i < files.Length; i++)   
                uploadFile[i] = !objects2.ContainsKey(relativeFilename[i]) || uploadFile[i]; //... or it has never been uploaded at all
              
            for (int i = 0; i < files.Length; i++)//iterate from front so that ServerIP.txt is uploaded first. that way is has the lowest timeDate
                if(uploadFile[i])
                    await fileTransferUtility.UploadAsync(files[i], bucketName, relativeFilename[i]);

            int filesUploaded = 0;
            for (int i = 0; i < uploadFile.Length; i++)  
                if(uploadFile[i])
                    filesUploaded++;
             
            Chat("Upload "+filesUploaded.ToString()+ " files successfully."); 
        }

        private bool HandleHTTP(byte[] result, Socket connection) { 
            string request = Encoding.UTF8.GetString(result, 0, result.Length);
            string[] requestArray = request.Split(" "); 

            switch (requestArray[0]) {

                case ("GET"): 
                    string[] DirectoryPath = requestArray[1].Split("/");
                    string text_message = "Error 404 " + DirectoryPath[0] +" Page not found";
                    string responseBody = "";
                    string combinedTotalPackage = "";

                    switch (DirectoryPath[1]) {

                        case ("ActivateAccount"):
                            string username = DirectoryPath[2];
                            string ActivationKey = DirectoryPath[3];
                            bool success = DB.ActivateAccount(username, ActivationKey);
                            text_message = success ? "Activated" : "FailedToActivate";
                            text_message += "  " + username + " <br/> with Key: " + ActivationKey;
                            break;
                        default:
                            break;
                    } 

                    responseBody = "<!DOCTYPE html>" + "<html>" 
                        + "<head><title>Account Activation</title></head>" +
                        "<body><h1>" + text_message +"<img src = "+"path/to/your/image.jpg"+">"+ "</h1></body>"
                        + "</html>";
                    combinedTotalPackage = "HTTP/1.1 200 OK\r\n" + "Content-Type: text/html\r\n" + "Content-Length: " + Encoding.UTF8.GetByteCount(responseBody) + "\r\n"
                                    + "Connection: close\r\n" + "\r\n" + responseBody;

                    byte[] packageBytes = Encoding.UTF8.GetBytes(combinedTotalPackage);
                    connection.Send(packageBytes); 
                    return true;

            }


            return false; 
        }

        public static void SendEmail(string TargetMail, string username, string ActivationKey) {
            string filePath = @"C:\Users\Klauke\Documents\My Games\Corivi\EmailCredentials.txt";//temp, but not as dumb as uploading that to github

            // Read all lines from the file
            string[] lines = File.ReadAllLines(filePath); 
            string myMail = lines[0];
            string myPass = lines[1];

            SmtpClient client = new SmtpClient("mail.gmx.net", 587);  
            client.EnableSsl = true; // Enable SSL
            client.UseDefaultCredentials = false;
             
            client.Credentials = new NetworkCredential(myMail, myPass);
             
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(myMail);
            try {//test if the email is valid
                mailMessage.To.Add(TargetMail); 
            }
            catch (Exception) { 
                return;
            }
            mailMessage.Body = "test, (das ist unsere IP adresse, nicht wundern http://80.129.131.87:16501/"+"ActivateAccount/"+username+"/"+ActivationKey;
            mailMessage.Subject = "Test Email";

            // Send the email
            try {
                client.Send(mailMessage); 
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task<int> LevelupCharacter(int requestedLevel, int itemIndex, int userGuid) {
             
            Chat("Character wants to level up to level " + requestedLevel.ToString());

            int characterXP = await DB.GetCharacterXP(itemIndex, userGuid);
            int userXP = await DB.GetUserXP(userGuid);
            int actualLevel = (int)Math.Sqrt((double)characterXP / 10); // Sum (n+n-1) starting at n= 1 =  n^2 , so converting back is just a sqare root
            int nextLevel = actualLevel + 1;
            int requiredXP = (nextLevel * 2 - 1) * 10; //  (n+n-1) is equal to 2n-1

            if (requestedLevel - 1 != actualLevel) {
                Chat("ERROR, client had different level data, maybe clicked too fast");
                return -1;
            }

            if (userXP < requiredXP)
                return -1;

            DB.SetLevelup(itemIndex, userGuid, requiredXP);
            int xp_remaining = userXP - requiredXP;
            return xp_remaining;
        }
         

        public async Task<byte[]> GetFriendData(int playerID) {
            int[] friendGuids = ReadFriendlistIDs(playerID);

            int friendCount = 0;//count friends and ignore zeroes 
            for (int i = 0; i < friendGuids.Length; i++) {
                if (friendGuids[i] != 0) {
                    friendGuids[friendCount] = friendGuids[i];//condense the array 
                    friendCount++;
                }
            }
            string[] names = await DB.getNames(friendGuids, friendCount);

            byte[] flistWithZeroes = new byte[4 + (friendCount * (4))];
            Buffer.BlockCopy(BitConverter.GetBytes(friendCount), 0, flistWithZeroes, 0, 4);//write friendcount
            Buffer.BlockCopy(friendGuids, 0, flistWithZeroes, 4, friendCount * 4);//write all IDs 

            byte[] friendsList = StringArrayToByte(names);

            return ServerHelper.CombineBytes(flistWithZeroes, friendsList);
        }

        private static int[] ReadFriendlistIDs(int playerID) {
            string path = Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "/My Games/Corivi/GameServer/" + "Friendlists/" + playerID + ".fl";

            int[] friendGuids = new int[60];
            if (File.Exists(path)) {//read friendsdata from the file
                byte[] allBytes = File.ReadAllBytes(path);//read all bytes and declare the arrays where the bytes will be copied to

                Buffer.BlockCopy(allBytes, 0, friendGuids, 0, 240);
            }

            return friendGuids;
        }

        public void SaveFriend(int playerID, int friendID) { 
            int[] friendGuids = ReadFriendlistIDs(playerID);
            for (int i = 0; i < friendGuids.Length; i++)  
                if (friendGuids[i] == friendID)  
                    return;

            for (int i = 0; i < friendGuids.Length; i++)
                if (friendGuids[i] == 0) {
                    friendGuids[i] = friendID;
                    break; 
                }

            SaveFriendlistIDs(playerID, friendGuids);
        }

        private void SaveFriendlistIDs(int playerID, int[] flist) {
            string path = Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "/My Games/Corivi/GameServer/" + "Friendlists/" + playerID + ".fl";
            byte[] saveData = new byte[flist.Length * 4];
            Buffer.BlockCopy(flist,0, saveData, 0, flist.Length*4);
            File.WriteAllBytes(path, saveData);
        }

        public void RemoveFriend(int playerID, int friendID) {
            int[] friendGuids = ReadFriendlistIDs(playerID);
            for (int i = 0; i < friendGuids.Length; i++)  
                if (friendGuids[i] == friendID)
                    friendGuids[i] = 0;


            SaveFriendlistIDs(playerID, friendGuids);

        }



        private byte[] StringArrayToByte(string[] names) {
            int byteCount = 0;//4bytes for friendcount, 4 bytes per ID + 1byte per status  

            byte[] uncutArray = new byte[5000];

            for (int i = 0; i < names.Length; i++) {
                byte[] encodedName = Encoding.UTF8.GetBytes(names[i]);

                //write all names
                Buffer.BlockCopy(BitConverter.GetBytes(encodedName.Length), 0, uncutArray, byteCount, 4);
                byteCount += 4;
                Buffer.BlockCopy(encodedName, 0, uncutArray, byteCount, encodedName.Length);
                byteCount += encodedName.Length;
            }

            byte[] friendsList = new byte[byteCount];
            Buffer.BlockCopy(uncutArray, 0, friendsList, 0, byteCount); //write all data to a new condensed array
            return friendsList;
        }

        private async void Login(string email, string encryptedPassword, Socket connection, int clientToken, int netID) {// netID for game doesnt log in, just checks credentials


            //Step1: Retrieve the Guid and the Name of the Account. If we cant, the login failed 
            LoginNameID GuidAndName = await DB.Login(email, encryptedPassword);

            if (GuidAndName.ID == -1) {
                ServerHelper.Send(ref connection, PacketTypeServer.loginFailed, Array.Empty<byte>());
                return;
            }

            byte[] friendslistdata = await GetFriendData(GuidAndName.ID);
            byte[] friendrequests = await DB.GetFriendrequests(GuidAndName.ID);
            string[] friendrequestNames = await DB.getNames(friendrequests);

            byte[] requestnames = StringArrayToByte(friendrequestNames);
            Chat("A Client logged int: GUId = " + GuidAndName.ID.ToString() + " , name = " + GuidAndName.name + " , email = " + email);




            //this is what actually logs in the client. but the game server will also check logins, 
            //the game server will send a netID so that it can reidentify the gameClient who logs in
            //we use the existence of the netID as an indicator that we dont actually wanne log in
            if (netID == 0)
                loginSessions[GuidAndName.ID] = new(clientToken, connection); //add or update dictionary, but only if the netID is 0, because only gameServer sends netIDs to reidentify the player

            CustomizedCharacter[] characterList = await DB.GetCharacters(GuidAndName.ID); 
            int userXP = await DB.GetUserXP(GuidAndName.ID);
            byte[] namebytes = Encoding.UTF8.GetBytes(GuidAndName.name);

            List<byte[]> data = new();
            data.Add(BitConverter.GetBytes(GuidAndName.ID));
             

            //add all character data
            data.Add(BitConverter.GetBytes(characterList.Length));
            for (int i = 0; i < characterList.Length; i++) {
                byte[] byteDataChar = characterList[i].ToByte();
                data.Add(BitConverter.GetBytes(byteDataChar.Length));
                data.Add(byteDataChar);
                data.Add(BitConverter.GetBytes(characterList[i].characterType));
                data.Add(BitConverter.GetBytes(characterList[i].Guid));
                data.Add(BitConverter.GetBytes(characterList[i].Level));
            }

            data.Add(BitConverter.GetBytes(netID));
            data.Add(BitConverter.GetBytes(namebytes.Length));
            data.Add(namebytes);
            data.Add(BitConverter.GetBytes(userXP));
            data.Add(friendslistdata);
            data.Add(BitConverter.GetBytes(friendrequests.Length / 4));
            data.Add(friendrequests);
            data.Add(requestnames);

            byte[] combinedData = ServerHelper.Pack(data);
            ServerHelper.Send(ref connection, PacketTypeServer.LoginSuccessfull, combinedData);
            if(netID!=0)
                Chat("Sent " + combinedData.Length + "bytes of login data to gameServer");

        }

        private void Chat(string message) {
            this.Dispatcher.Invoke(() =>
            {
                Chatoutput.Add(message);
                OnPropertyChanged(nameof(Chatoutput));  
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonTerminate_Click(object sender, RoutedEventArgs e) {
            UploadFile();

            for (int i = 0; i < connections.Count; i++) {
                connections[i].Close();
            }
            connections.Clear();
        }

        private void ButtonBuildCharacterFiles_Click(object sender, RoutedEventArgs e) {
            StaticData.BuildBaseValues();
        }

        private void ButtonResetInventories_Click(object sender, RoutedEventArgs e) { 
            DB.ResetInventories();
        }
    }
}
