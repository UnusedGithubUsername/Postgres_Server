using System.IO;
using static Server.Enums;

namespace Server {
    internal static class StaticData {

        public static void BuildBaseValues() { //create the files that we will network from the baseValues Array  
            for (int i = 0; i < baseValues.Length; i++)  
                File.WriteAllBytes(ServerHelper.BasePath + "CharacterBaseValues\\" + i.ToString() + ".characterData", baseValues[i].ToByte());

            for (int i = 0; i < skills.Length; i++)
                File.WriteAllBytes(ServerHelper.BasePath + "Skills\\" + ((int)skills[i].ID).ToString() + ".skill", skills[i].ToByte());



        }


        static SkillFile[] skills = {
            new SkillFile(
                SkillID.None,
                " ",
                "Empty Skillslot"
                ),

            new SkillFile(
                SkillID.Strength,
                "Strength",
                "+5 Strength"
                ),

            new SkillFile(
                SkillID.Vitality,
                "Vitality",
                "+5 Vitality"
                ),

            new SkillFile(
                SkillID.Intelligence,
                "Intelligence",
                "+5 Intelligence"
                ),

            new SkillFile(
                SkillID.Wisdom,
                "Wisdom",
                "+5 Wisdom"
                ),

            new SkillFile(
                SkillID.ExplosiveSpells,
                "Explosive Spells",
                "Many Spells gain an AoE effect"
                ),

                        
            new SkillFile(
                SkillID.SecondaryAttunement,
                "Attunement",
                "You may choose a second attuned school of magic"
                ),

                                    
            new SkillFile(
                SkillID.NeuralFastpass,
                "Neural Fastpass",
                "Consecutive Ability usage reduces its casting cost by 20%. Up to 3 Stacks for one Skill and one Spell"
                ),
                                                
            new SkillFile(
                SkillID.ExplosiveArrows,
                "Explosive Arrows",
                "Explosive Spells can be applied to Bows"
                ),
                                                            
            new SkillFile(
                SkillID.PhysResist,
                "Hard Skinned",
                "10% Physical Resistance"
                ),

            new SkillFile(
                SkillID.PhysImmun,
                "Tough as Nails",
                "10% Physical Immunity"
                ),

        //   new SkillFile(
        //       SkillID.testskill7,
        //       "Wardancer",
        //       "10% Physical Immunity"
        //       ),
        //
        };

        static CustomizedCharacter[] baseValues = {
            new CustomizedCharacter(
                new byte[] {1, 1, 1, 1},
                new byte[] {0, 0, 0, 0},
                new byte[] {0,0,0,0,0,0,0,0,0,0},
                "ZeroStatsCharacter",
                "AbilityName",
                "this is an empty shell, this is an ability description"
                ),

            new CustomizedCharacter(
                new byte[] {10, 15, 15, 20},
                new byte[] {0, 1, 2, 1},
                new byte[] {0,0,0,0,0,0,0,0,0,0},
                "Zealot",
                "Primal Attunement",
                "Your second Attunement will target the same school"
                ),

            new CustomizedCharacter(
                new byte[] {10, 15, 15, 20},
                new byte[] {0, 1, 2, 1},
                new byte[] {0,0,0,0,0,0,0,0,0,0},
                "Ketzer",
                "Heretical Teachings",
                "Your Attuned school of magic is inversed (e.g. Black -> White). The second attunement is random"
                ),

            new CustomizedCharacter(
                new byte[] {20, 15, 15, 10},
                new byte[] {0, 1, 2, 1},
                new byte[] {2,3,1,0,0,0,0,0,0,0},
                "Wardancer",
                "Wardance",
                "Two Skills and no Spells benefit from neural fastpass"
                ),

            new CustomizedCharacter(
                new byte[] {20, 18, 12, 10},
                new byte[] {0, 1, 2, 1},
                new byte[] {1,0,0,5,0,0,2,0,0,0},
                "qweqweqwe",
                "AbilityName",
                "AbilityDescription"
                ),

            new CustomizedCharacter(
                new byte[] {20, 18, 12, 10},
                new byte[] {0, 1, 2, 1},
                new byte[] {1,0,0,5,0,0,2,0,0,0},
                "qweqweqwe",
                "AbilityName",
                "You gain no fevor stacks"
                ),
            
            new CustomizedCharacter(
                new byte[] {20, 18, 12, 10},
                new byte[] {0, 1, 2, 1},
                new byte[] {1,0,0,5,0,0,2,0,0,0},
                "qweqweqwe",
                "AbilityName",
                "AbilityDescription"
                ),

            new CustomizedCharacter(
                new byte[] {20, 18, 12, 10},
                new byte[] {0, 1, 2, 1},
                new byte[] {1,0,0,5,0,0,2,0,0,0},
                "Leper",
                "Superspreader",
                "Your latest Debuff does not expire and imbues your weapon"
                ),

                            
            new CustomizedCharacter(
                new byte[] {20, 18, 12, 10},
                new byte[] {0, 1, 2, 1},
                new byte[] {1,0,0,5,0,0,2,0,0,0},
                "qweqweqwe",
                "qweqweqwe",
                "Learning Spells increases your life instead of mana"
                )

        };

        public static CustomizedCharacter GetCharacterBaseValues(int index) {

            index -= 1000; //character itemIDs start at 1001
            //TODO, make a constructor for base values of chustomized character, make an array of these thigs in this class 
            if (index < 1 || index > baseValues.Length)
                index = 0; //i.e. return  0 0 0 

            return baseValues[index];
        } 


    }
}
