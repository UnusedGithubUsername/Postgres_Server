using System.Net.Sockets;

namespace Server {
    public static class Enums {

        public enum Skill {
            MassSpells = 0,  // Skill that enables AoE on Spells
            ArcaneKnowledge = 1, //learn level 3 spells
            PrimalAttunement = 2, //Choose a second attuned magic school, your first attuned school decreases Spellslotcost by 2;
            NeuralFastpass = 3, //Consecutive uses of Skills and Spells reduces the casting cost for further uses by 20%, One Skill and one spell can have up to 3 Stacks at the same time

        }

        public enum PacketTypeClient {
            KeepAlive = 0,
            RequestKey = 1,
            requestWithToken,
            Login = 3,
            RegisterUser = 4,
            ForewardLoginPacket,
            Friendrequest,
            Message = 9
        }

        public enum FriendrequestAction {
            RequestFriendship,
            Accept,
            Deny,
            Block,
            Remove
        }

        public enum PacketTypeServer {

            publicKeyPackage,
            LoginSuccessfull,
            loginFailed,
            CharacterData,
            levelupSuccessfull,
            Message = 9
        }
        
        public struct int2 {
            public int2(int token, Socket con) {
                sessionID = token;
                connection = con;
            }
            public int sessionID;
            public Socket connection;
        }

        public enum SkillID {
            None = 0,

            Strength = 1,
                ExplosiveArrows = 10,
                

            Vitality = 2,
                PhysResist = 20,
                    PhysImmun = 200,

            Intelligence = 3,
                ExplosiveSpells = 30,

            Wisdom = 4,
                SecondaryAttunement = 40,
                OneWithEverything = 41,


            testskill7 = 6,
            testskill8 = 7,
            testskill9 = 8,
            //Nothing = 9, //abilities with 901 to 999 have no prequisites
                    NeuralFastpass = 90,//enums above 256 dont work... because skills are encoded as byte which caps at 127

        }
    }
}
