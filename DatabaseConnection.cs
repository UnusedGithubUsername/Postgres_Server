using Npgsql;
using Server;
using System;
using System.Collections.Generic; 
using System.Threading.Tasks; 

public class DatabaseConnection
{ 
    private NpgsqlConnection database;

    //check login credentials
    private NpgsqlCommand Cmd_Logincheck;
    private NpgsqlParameter CmdP_Logincheck_email;
    private NpgsqlParameter CmdP_Logincheck_password; //hashed salted and encrypted pw, dont forget to decrypt before asking db

    //retrieve inventory that we can send to a client
    private NpgsqlCommand Cmd_getItems;
    private NpgsqlParameter CmdP_getItems_UserGUID;

    //get the type of a character from a player to validate a clients request
    private NpgsqlCommand Cmd_getCharacterType;
    private NpgsqlParameter CmdP_getCharacterType_PlayerGUID;
    private NpgsqlParameter CmdP_getCharacterType_ItemIndex;

    //get the type of a character from a player to validate a clients request
    private NpgsqlCommand Cmd_getCharacterXp;
    private NpgsqlParameter CmdP_getCharacterXp_PlayerGUID;
    private NpgsqlParameter CmdP_getCharacterXp_CharacterIndex;

    //get the type of a character from a player to validate a clients request
    private NpgsqlCommand Cmd_getUserXp;
    private NpgsqlParameter CmdP_getUserXp_PlayerGUID;

    //levelup a character by one level
    private NpgsqlCommand Cmd_levelup;
    private NpgsqlParameter CmdP_levelup_XP;
    private NpgsqlParameter CmdP_levelup_GUID;
    private NpgsqlParameter CmdP_levelup_CharacterIndex;

    private NpgsqlCommand Cmd_GetNamelist;
    private NpgsqlParameter CmdP_GetNamelist_GuidArray;

    private NpgsqlCommand Cmd_Friendrequest;
    private NpgsqlParameter CmdP_Friendrequest_userID;
    private NpgsqlParameter CmdP_Friendrequest_requestor;

    private NpgsqlCommand Cmd_GetFriendrequest;
    private NpgsqlParameter CmdP_GetFriendrequest_GUID;

    private NpgsqlCommand Cmd_FriendrequestDeny;
    private NpgsqlParameter CmdP_FriendrequestDeny_userID;
    private NpgsqlParameter CmdP_FriendrequestDeny_requestor;
    
    private NpgsqlCommand Cmd_FriendrequestBlock;
    private NpgsqlParameter CmdP_FriendrequestBlock_userID;
    private NpgsqlParameter CmdP_FriendrequestBlock_requestor;


    private NpgsqlCommand Cmd_CheckUsername;
    private NpgsqlParameter CmdP_CheckUsername_username;

    private NpgsqlCommand Cmd_NewUser;
    private NpgsqlParameter CmdP_NewUser_username;
    private NpgsqlParameter CmdP_NewUser_email;
    private NpgsqlParameter CmdP_NewUser_password;
    private NpgsqlParameter CmdP_NewUser_activationKey;

    private NpgsqlCommand Cmd_ActivateUser;
    private NpgsqlParameter CmdP_ActivateUser_username; 
    private NpgsqlParameter CmdP_ActivateUser_confirmationKey;


    public static int[,] baseItems = {
                {1001, 0},
                {1002, 0},
                {1003, 0},
                {1, 2000}
            };

    public DatabaseConnection() {

        using NpgsqlDataSource dataSource = NpgsqlDataSource.Create("Host=localhost;Port=5432;Username=postgres;Password=qwert;Database=qqq_game");
        database = dataSource.OpenConnection();

        string preparedStatemenLogint = "SELECT GUId, username FROM users where email = (@email) ";// AND password = (@password);";
        Cmd_Logincheck = new NpgsqlCommand(preparedStatemenLogint, database);
        CmdP_Logincheck_email = Cmd_Logincheck.Parameters.Add(new NpgsqlParameter { ParameterName = "email", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        CmdP_Logincheck_password = Cmd_Logincheck.Parameters.Add(new NpgsqlParameter { ParameterName = "password", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        Cmd_Logincheck.Prepare();

        string preparedStatementItems = "SELECT item_guid, item_id, xp FROM items where user_guid = (@user_guid) AND item_id != 1;";
        Cmd_getItems = new NpgsqlCommand(preparedStatementItems, database);
        CmdP_getItems_UserGUID = Cmd_getItems.Parameters.Add(new NpgsqlParameter { ParameterName = "user_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_getItems.Prepare();

        string preparedStatementCharacterType = "SELECT item_id FROM items WHERE user_guid = (@user_guid) AND item_guid = (@item_guid);";
        Cmd_getCharacterType = new NpgsqlCommand(preparedStatementCharacterType, database);
        CmdP_getCharacterType_PlayerGUID = Cmd_getCharacterType.Parameters.Add(new NpgsqlParameter { ParameterName = "user_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        CmdP_getCharacterType_ItemIndex = Cmd_getCharacterType.Parameters.Add(new NpgsqlParameter { ParameterName = "item_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_getCharacterType.Prepare();

        string preparedStatementCharacterXp = "SELECT xp FROM items WHERE user_guid = (@user_guid) AND item_guid = (@item_guid);";
        Cmd_getCharacterXp = new NpgsqlCommand(preparedStatementCharacterXp, database);
        CmdP_getCharacterXp_PlayerGUID = Cmd_getCharacterXp.Parameters.Add(new NpgsqlParameter { ParameterName = "user_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        CmdP_getCharacterXp_CharacterIndex = Cmd_getCharacterXp.Parameters.Add(new NpgsqlParameter { ParameterName = "item_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_getCharacterXp.Prepare();

        string preparedStatementUserXp = "SELECT xp FROM items WHERE user_guid = (@user_guid) AND item_id = 1;";
        Cmd_getUserXp = new NpgsqlCommand(preparedStatementUserXp, database);
        CmdP_getUserXp_PlayerGUID = Cmd_getUserXp.Parameters.Add(new NpgsqlParameter { ParameterName = "user_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_getUserXp.Prepare();

        string preparedStatementLevelup =
            "UPDATE items SET xp = xp + (@xp_value) WHERE user_guid = (@user_guid) AND item_guid = (@item_guid);" +
            "UPDATE items SET xp = xp - (@xp_value) WHERE user_guid = (@user_guid) AND item_id = 1;";
        Cmd_levelup = new NpgsqlCommand(preparedStatementLevelup, database);
        CmdP_levelup_XP = Cmd_levelup.Parameters.Add(new NpgsqlParameter { ParameterName = "xp_value", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        CmdP_levelup_GUID = Cmd_levelup.Parameters.Add(new NpgsqlParameter { ParameterName = "user_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        CmdP_levelup_CharacterIndex = Cmd_levelup.Parameters.Add(new NpgsqlParameter { ParameterName = "item_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_levelup.Prepare();

        string preparedStatementGetNamelist = "SELECT GUId, username FROM users WHERE GUId = ANY(@guids)";
        Cmd_GetNamelist = new NpgsqlCommand(preparedStatementGetNamelist, database);
        CmdP_GetNamelist_GuidArray = Cmd_GetNamelist.Parameters.Add(new NpgsqlParameter { ParameterName = "guids", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_GetNamelist.Prepare();

        //insert a column with a userID, a reqID and a bool. Create the column to insert, but only if we can not select 1 from the table with the user/reqID combination
        string preparedStatementFriendrequest = "INSERT INTO friendrequests (userID, requestorID, blocked) SELECT @user_guid, @requestor_guid, false WHERE NOT EXISTS (SELECT 1 FROM friendrequests WHERE userID = @user_guid AND requestorID = @requestor_guid)";
        Cmd_Friendrequest = new NpgsqlCommand(preparedStatementFriendrequest, database);
        CmdP_Friendrequest_userID = Cmd_Friendrequest.Parameters.Add(new NpgsqlParameter { ParameterName = "user_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        CmdP_Friendrequest_requestor = Cmd_Friendrequest.Parameters.Add(new NpgsqlParameter { ParameterName = "requestor_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_Friendrequest.Prepare();

        //insert a column with a userID, a reqID and a bool. Create the column to insert, but only if we can not select 1 from the table with the user/reqID combination
        string preparedStatementFriendrequestDeny = "DELETE FROM friendrequests WHERE userID = @user_guid AND requestorID = @requestor_guid";
        Cmd_FriendrequestDeny = new NpgsqlCommand(preparedStatementFriendrequestDeny, database);
        CmdP_FriendrequestDeny_userID = Cmd_FriendrequestDeny.Parameters.Add(new NpgsqlParameter { ParameterName = "user_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        CmdP_FriendrequestDeny_requestor = Cmd_FriendrequestDeny.Parameters.Add(new NpgsqlParameter { ParameterName = "requestor_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_FriendrequestDeny.Prepare();

        //insert a column with a userID, a reqID and a bool. Create the column to insert, but only if we can not select 1 from the table with the user/reqID combination
        string preparedStatementFriendrequestBlock = "update friendrequests SET blocked = 't' WHERE userID = @user_guid AND requestorID = @requestor_guid";
        Cmd_FriendrequestBlock = new NpgsqlCommand(preparedStatementFriendrequestBlock, database);
        CmdP_FriendrequestBlock_userID = Cmd_FriendrequestBlock.Parameters.Add(new NpgsqlParameter { ParameterName = "user_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        CmdP_FriendrequestBlock_requestor = Cmd_FriendrequestBlock.Parameters.Add(new NpgsqlParameter { ParameterName = "requestor_guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_FriendrequestBlock.Prepare();

        string preparedStatementGetFriendrequest = "SELECT requestorid FROM friendrequests WHERE userid = (@guid) AND blocked = 'f'";
        Cmd_GetFriendrequest = new NpgsqlCommand(preparedStatementGetFriendrequest, database);
        CmdP_GetFriendrequest_GUID = Cmd_GetFriendrequest.Parameters.Add(new NpgsqlParameter { ParameterName = "guid", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        Cmd_GetFriendrequest.Prepare();

        string preparedStatementCheckUsername = "SELECT COUNT(*) FROM users WHERE username = @username";
        Cmd_CheckUsername = new NpgsqlCommand(preparedStatementCheckUsername, database);
        CmdP_CheckUsername_username = Cmd_CheckUsername.Parameters.Add(new NpgsqlParameter { ParameterName = "username", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        Cmd_CheckUsername.Prepare();

        string preparedStatementNewUser = "INSERT INTO users(email, username, password, confirmationkey) VALUES(@email, @username, @password, @confirmationkey)";
        Cmd_NewUser = new NpgsqlCommand(preparedStatementNewUser, database);
        CmdP_NewUser_username = Cmd_NewUser.Parameters.Add(new NpgsqlParameter { ParameterName = "username", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        CmdP_NewUser_email = Cmd_NewUser.Parameters.Add(new NpgsqlParameter { ParameterName = "email", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        CmdP_NewUser_password = Cmd_NewUser.Parameters.Add(new NpgsqlParameter { ParameterName = "password", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        CmdP_NewUser_activationKey = Cmd_NewUser.Parameters.Add(new NpgsqlParameter { ParameterName = "confirmationkey", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        Cmd_NewUser.Prepare();

        string preparedStatementActivateUser = "Update users Set activated = true WHERE username = @username AND confirmationkey = @confirmationkey";
        Cmd_ActivateUser = new NpgsqlCommand(preparedStatementActivateUser, database);
        CmdP_ActivateUser_username = Cmd_ActivateUser.Parameters.Add(new NpgsqlParameter { ParameterName = "username", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        CmdP_ActivateUser_confirmationKey = Cmd_ActivateUser.Parameters.Add(new NpgsqlParameter { ParameterName = "confirmationkey", NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        Cmd_ActivateUser.Prepare();
    }

    public void RequestFriendship(int friendID, int requestorID) {
        CmdP_Friendrequest_userID.Value = friendID; 
        CmdP_Friendrequest_requestor.Value = requestorID;
        Cmd_Friendrequest.ExecuteNonQuery();
    }
     
    internal void BlockFriendship(int user, int friendGuid) {
        CmdP_FriendrequestBlock_userID.Value = user;
        CmdP_FriendrequestBlock_requestor.Value = friendGuid;
        Cmd_FriendrequestBlock.ExecuteNonQuery();
    }

    internal int DenyFriendship(int user, int friendGuid) {
        CmdP_FriendrequestDeny_userID.Value = user;
        CmdP_FriendrequestDeny_requestor.Value = friendGuid;
        return Cmd_FriendrequestDeny.ExecuteNonQuery();
    }
    public async Task<byte[]> GetFriendrequests(int guid) {
        CmdP_GetFriendrequest_GUID.Value = guid;
        await using NpgsqlDataReader reader = Cmd_GetFriendrequest.ExecuteReader();


        int[] requestor_ids = new int[5000];
        int friendrequests = 0;
        while (reader.Read()) { 
            requestor_ids[friendrequests] = reader.GetInt32(0);  
            friendrequests++; 
        }
        reader.Close();

        byte[] requests = new byte[friendrequests * 4];
        Buffer.BlockCopy(requestor_ids, 0, requests, 0, friendrequests * 4);

        return requests;
    }

    public async Task<string[]> getNames(int[] guids, int count) {
        CmdP_GetNamelist_GuidArray.Value = guids;
        string[] names = new string[count];

        await using NpgsqlDataReader reader = Cmd_GetNamelist.ExecuteReader();
        for (int i = 0; i < count; i++) {
            if (!reader.Read())
                break;

            int guid = reader.GetInt32(0);
            string name = reader.GetString(1);

            int index = -1; //not -1 so we dont get exceptions
            for (int j = 0; j < count; j++) {
                if(guids[j] == guid)
                    index = j;//find the appropriate index in the guid array
            }
            names[index] = name;
        }     
        reader.Close();
        return names;
    }

    public async Task<string[]> getNames(byte[] guids) {

        int count = guids.Length / 4;
        int[] Guids = new int[count];
        Buffer.BlockCopy(guids, 0, Guids, 0, guids.Length);

        CmdP_GetNamelist_GuidArray.Value = Guids;
        string[] names = new string[count];

        await using NpgsqlDataReader reader = Cmd_GetNamelist.ExecuteReader();
        for (int i = 0; i < count; i++) {
            if (!reader.Read())
                break;

            int guid = reader.GetInt32(0);
            string name = reader.GetString(1);

            int index = -1; //not -1 so we dont get exceptions
            for (int j = 0; j < count; j++) {
                if (Guids[j] == guid)
                    index = j;//find the appropriate index in the guid array
            }
            names[index] = name;
        }
        reader.Close();
        return names;
    }



    public void SetLevelup(int itemIndex, int userGuid, int xp_value) {
        CmdP_levelup_CharacterIndex.Value = itemIndex;
        CmdP_levelup_GUID.Value = userGuid;
        CmdP_levelup_XP.Value = xp_value;
        Cmd_levelup.ExecuteNonQuery();
    }

    public async Task<int> GetCharacterXP(int itemIndex, int userGuid) {
        CmdP_getCharacterXp_CharacterIndex.Value = itemIndex;
        CmdP_getCharacterXp_PlayerGUID.Value = userGuid;
        return await GetSingleValueFromQuery(Cmd_getCharacterXp);
    }

    public async Task<int> GetUserXP(int userGuid) {
        CmdP_getUserXp_PlayerGUID.Value = userGuid;
        return await GetSingleValueFromQuery(Cmd_getUserXp);
    }

    public async Task<int> GetItemType(int userGuid, int item_guid) {
        CmdP_getCharacterType_PlayerGUID.Value = userGuid;
        CmdP_getCharacterType_ItemIndex.Value = item_guid;
        return await GetSingleValueFromQuery(Cmd_getCharacterType);
    }



    public async Task<int> GetSingleValueFromQuery(NpgsqlCommand cmd) {
        await using NpgsqlDataReader reader = cmd.ExecuteReader();
        int value = reader.Read() ? reader.GetInt32(0) : -1;//if data is available to be read, that means we got itemID back
        reader.Close();
        return value;
    }

    
    public async Task<LoginNameID> Login(string name, string encryptedPassword) {
        CmdP_Logincheck_email.Value = name;
        CmdP_Logincheck_password.Value = encryptedPassword;
        await using NpgsqlDataReader reader = Cmd_Logincheck.ExecuteReader();

        LoginNameID returnVal = new();
        if (!reader.Read())//if no matching account is found, the login failed
        {
            returnVal.ID = -1;
        }
        else {
            returnVal.ID = reader.GetInt32(0); //get the first value in the array (the array is 1 long, and the first value is the guid
            returnVal.name = reader.GetString(1);
        }
        reader.Close();
        return returnVal;
    }

    public async Task<CustomizedCharacter[]> GetCharacters(int iD) {
        //now retrieve the clients items
        CmdP_getItems_UserGUID.Value = iD;
        await using NpgsqlDataReader itemReader = Cmd_getItems.ExecuteReader();

        int characterCount = 0;
        List<int[]> itemList = new();
        while (itemReader.Read()) {
            int[] idata = new int[3];
            for (int i = 0; i < 3; i++)  
                idata[i] = itemReader.GetInt32(i);  

            itemList.Add(idata);
            characterCount++;
        }
        itemReader.Close();

        CustomizedCharacter[] CharacterList = new CustomizedCharacter[characterCount];
        for (int i = 0; i < itemList.Count; i++) {
            CharacterList[i] = CharacterDB.getCharacterData(iD, (itemList[i])[0], (itemList[i])[1]);
            int characterLevel = (int)Math.Sqrt((itemList[i])[2] / 10);
            CharacterList[i].SetIDs((itemList[i])[1], (itemList[i])[0], characterLevel);
        }
        return CharacterList; 
    } 

    public async void ResetInventories() {
        
        //1) TRUNCATE TABLE items
        using NpgsqlCommand cmd0 = new NpgsqlCommand("TRUNCATE TABLE items", database);
        cmd0.ExecuteNonQuery();


        //2) get all guids from table. 
        List<int> playerGuids = new List<int>();

        using NpgsqlCommand cmd = new NpgsqlCommand("SELECT guid FROM users", database);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

        while (reader.Read()) {
            playerGuids.Add(reader.GetInt32(0));
        }
        reader.Close();

        for (int i = 0; i < playerGuids.Count; i++) {
            AddBaseCharactersToUserInventory(playerGuids[i]);
        }
    }

    private void AddBaseCharactersToUserInventory(int playerGuid) {
        for (int j = 0; j < baseItems.GetLength(0); j++) {
            string addItem = "INSERT INTO items (user_guid, item_id, xp, level ) VALUES (" + playerGuid.ToString() + ", " + baseItems[j, 0].ToString() + ", " + baseItems[j, 1].ToString() + ", 1)";
            using NpgsqlCommand cmd2 = new NpgsqlCommand(addItem, database);
            cmd2.ExecuteNonQuery();
        }
    }

    internal async void CreateAccount(string mail, string password, string username, string activationKey) {
        CmdP_NewUser_email.Value = mail;
        CmdP_NewUser_password.Value = password;
        CmdP_NewUser_username.Value = username;
        CmdP_NewUser_activationKey.Value = activationKey;
        Cmd_NewUser.ExecuteNonQuery();

        LoginNameID nameID = await Login(mail, password); 
        if(nameID.name == username)
            AddBaseCharactersToUserInventory(nameID.ID);
    }

    public bool CheckIfAccountExists(string username) {
        CmdP_CheckUsername_username.Value = username; 
        long numOfAccsWithThatName = (long)Cmd_CheckUsername.ExecuteScalar();
        return numOfAccsWithThatName == 0; //return if the number of times the username exists is 0
    }

    internal bool ActivateAccount(string username, string activationKey) {
        CmdP_ActivateUser_username.Value = username;
        CmdP_ActivateUser_confirmationKey.Value = activationKey;
        int affectedRows = Cmd_ActivateUser.ExecuteNonQuery();
        return affectedRows>0;
    }
}
public struct LoginNameID {
    public int ID;
    public string name;
}
