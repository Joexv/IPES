using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Diagnostics;
using IPES;

namespace InstantExport
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            characterValues = ReadTableFile(System.Windows.Forms.Application.StartupPath + @"\Config\Table.ini");
            characterValuesalt = ReadTableFile(System.Windows.Forms.Application.StartupPath + @"\Config\Table2.ini");
        }
        OpenFileDialog ofd = new OpenFileDialog();
        OpenFileDialog ofd2 = new OpenFileDialog();
        OpenFileDialog ofd3 = new OpenFileDialog();
        string currentROM;
        string BinFile;
        string TRFCode;
        string PKMNFile;
        string gameCode;
        string gameName;
        string gameType;
        private uint itemTable;
        private ushort numberOfItems;
        private uint pokemonNamesLocation;
        private ushort numberOfPokemon;
        private uint PokemonStats;
        private uint TypeNames;
        private ushort NumberofTypes;
        private uint TRFpokemonNamesLocation;
        private ushort TRFnumberOfPokemon;
        private uint TRFPokemonStats;
        private Dictionary<byte, char> characterValues;
        private Dictionary<byte, char> characterValuesalt;

        //Open Dialog
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ofd.Filter = "GBA ROM (*.gba)|*.gba"; //Opens GBA File
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                currentROM = ofd.FileName;
                using (BinaryReader br = new BinaryReader(File.OpenRead(currentROM)))
                {
                    br.BaseStream.Seek(0xAC, SeekOrigin.Begin);
                    gameCode = Encoding.ASCII.GetString(br.ReadBytes(4));
                }
                ParseINI(System.IO.File.ReadAllLines(System.Windows.Forms.Application.StartupPath + @"\Config\ROMS.ini"), gameCode);  //Parses ROMS ini to obtain offsets
                string[] gameCodeArray = { "AXVE", "AXPE", "BPRE", "BPGE", "BPEE" };
                if (gameCodeArray.Contains(gameCode))
                {
                    comboBox1.Items.Clear();
                    for (uint i = 0; i <= numberOfPokemon; i++)
                    {
                        if (i <= numberOfPokemon)
                            comboBox1.Items.Add(ROMCharactersToString(10, (uint)(0xB * i + pokemonNamesLocation)));
                    }
                    StatOffset.Text = "0x" + PokemonStats.ToString("x4");
                    NameOffset.Text = "0x" + pokemonNamesLocation.ToString("x4");
                    ofd2.Filter = "InstaTransfer TRF (*.TRF)|*.TRF"; //Opens TRF file
                    if (ofd2.ShowDialog() == DialogResult.OK)
                    {
                        BinFile = ofd2.FileName;
                        comboBox2.Items.Clear();
                        using (BinaryReader br2 = new BinaryReader(File.OpenRead(BinFile)))
                        {
                            br2.BaseStream.Seek(0xAC, SeekOrigin.Begin);
                            TRFCode = Encoding.ASCII.GetString(br2.ReadBytes(4));
                        }
                        ParseTRFINI(System.IO.File.ReadAllLines(System.Windows.Forms.Application.StartupPath + @"\Config\TRF.ini"), TRFCode);  //Parses TRF ini to obtain offsets
                        for (uint i = 0; i <= TRFnumberOfPokemon; i++)
                        {
                            if (i <= TRFnumberOfPokemon)
                                comboBox2.Items.Add(BinCharactersToString(10, (uint)(0xB * i + TRFpokemonNamesLocation)));
                        }
                    }
                    label66.Text = "Loaded ROM: " + ofd.SafeFileName + " | " + gameName;
                    comboBox2.Enabled = true;
                    comboBox1.Enabled = true;
                    TransferName.Enabled = true;
                    TransferStats.Enabled = true;
                    Next.Enabled = true;
                    ExportAll.Enabled = true;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    zeroOutTRFToolStripMenuItem.Enabled = true;
                }
            }
        }

        //Read TRF Pokemon data
        private void comboBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            //This entire thing runs fairly slow, reads entire data in about a second, seems to stall more than it should.
            BinaryReader br = new BinaryReader(File.OpenRead(BinFile));
            //Read Base Stats
            long Offset = TRFPokemonStats + (comboBox2.SelectedIndex * 0x1C);
            br.BaseStream.Seek(Offset, SeekOrigin.Begin);
            IHp.Text = "HP " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 1, SeekOrigin.Begin);
            IAtk.Text = "ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 2, SeekOrigin.Begin);
            IDef.Text = "DEF " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 3, SeekOrigin.Begin);
            ISAtk.Text = "S.ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 4, SeekOrigin.Begin);
            ISDef.Text = "S.Def " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 5, SeekOrigin.Begin);
            ISpd.Text = "SPD " + Convert.ToString(br.ReadByte());

            //Reads Types
            br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
            byte chunk = br.ReadByte();
            long TypeNames2 = TypeNames + (chunk * 7);
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                IType1.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                IType1.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }
            br.BaseStream.Seek(Offset + 7, SeekOrigin.Begin);
            chunk = br.ReadByte();
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                IType2.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                IType2.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }

            //Catch Rate
            br.BaseStream.Seek(Offset + 8, SeekOrigin.Begin);
            CatchIni.Text = "Catch Rate: " + Convert.ToString(br.ReadByte());
            //EXP Yield
            br.BaseStream.Seek(Offset + 9, SeekOrigin.Begin);
            IYeild.Text = "Yield: " + Convert.ToString(br.ReadByte());
            //Reads Gender and determines what percential it is
            br.BaseStream.Seek(Offset + 16, SeekOrigin.Begin);
            string male = "0";
            string female = "254";
            string genderless = "255";
            string half = "31";

            string Gender = Convert.ToString(br.ReadByte());
            if (Gender == male)
            {
                IGender.Text = "100% Male";
            }
            else if (Gender == female)
            {
                IGender.Text = "100% Female";
            }
            else if (Gender == genderless)
            {
                IGender.Text = "Genderless";
            }
            else if (Gender == half)
            {
                IGender.Text = "50/50";
            }
            else if (Gender == "127")
            {
                IGender.Text = "75% Female";
            }
            else
            { IGender.Text = Gender; }

            //Hatch Rate
            br.BaseStream.Seek(Offset + 17, SeekOrigin.Begin);
            IHatch.Text = "Hatch Rate: " + Convert.ToString(br.ReadByte());
            //Base Happiness
            br.BaseStream.Seek(Offset + 18, SeekOrigin.Begin);
            IHappy.Text = Convert.ToString(br.ReadByte());
            //Exp growth Rate
            br.BaseStream.Seek(Offset + 19, SeekOrigin.Begin);
            string EXPRate = Convert.ToString(br.ReadByte());
            IRate.Text = GetEXPRate(EXPRate);
            //Egg Group
            br.BaseStream.Seek(Offset + 20, SeekOrigin.Begin);
            string Egg1 = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 21, SeekOrigin.Begin);
            string Egg2 = Convert.ToString(br.ReadByte());
            //Obtains correct Egg group name to be displayed
            IEgg1.Text = GetEggGroup(Egg1);
            IEgg2.Text = GetEggGroup(Egg2);
            //Abilities
            br.BaseStream.Seek(Offset + 22, SeekOrigin.Begin);
            IAbil1.Text = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 23, SeekOrigin.Begin);
            IAbil2.Text = Convert.ToString(br.ReadByte());
            //Safari Zone Run Rate
            br.BaseStream.Seek(Offset + 24, SeekOrigin.Begin);
            ISafari.Text = "Safari Zone Rate: " + Convert.ToString(br.ReadByte());
            //Color
            br.BaseStream.Seek(Offset + 25, SeekOrigin.Begin);
            string Cint = Convert.ToString(br.ReadByte());
            IColor.Text = "Color: " + GetColor(Cint);
            //Item Reading is broken and removed, will fix in the future
            br.Close();

        }

        //Read ROM Pokemon data
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Reads Base Stats
            BinaryReader br = new BinaryReader(File.OpenRead(currentROM));
            long Offset = PokemonStats + (comboBox1.SelectedIndex * 28);
            br.BaseStream.Seek(Offset, SeekOrigin.Begin);
            Hp.Text = "HP " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 1, SeekOrigin.Begin);
            Atk.Text = "ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 2, SeekOrigin.Begin);
            Def.Text = "DEF " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 3, SeekOrigin.Begin);
            SAtk.Text = "S.ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 4, SeekOrigin.Begin);
            SDef.Text = "S.Def " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 5, SeekOrigin.Begin);
            Spd.Text = "SPD " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
            //Reads Types
            br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
            byte chunk = br.ReadByte();
            long TypeNames2 = TypeNames + (chunk * 7);
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                Type1.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                Type1.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }
            br.BaseStream.Seek(Offset + 7, SeekOrigin.Begin);
            chunk = br.ReadByte();
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                Type2.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                Type2.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }
            //Catch Rate
            br.BaseStream.Seek(Offset + 8, SeekOrigin.Begin);
            Catch.Text = "Catch Rate: " + Convert.ToString(br.ReadByte());
            //Exp yield
            br.BaseStream.Seek(Offset + 9, SeekOrigin.Begin);
            Yield.Text = "Yield: " + Convert.ToString(br.ReadByte());
            //Reds Gender then determines what percentile it falls under
            br.BaseStream.Seek(Offset + 16, SeekOrigin.Begin);
            string male = "0";
            string female = "254";
            string genderless = "255";
            string half = "31";
            string RGender = Convert.ToString(br.ReadByte());
            if (RGender == male)
            {
                Gender.Text = "100% Male";
            }
            else if (RGender == female)
            {
                Gender.Text = "100% Female";
            }
            else if (RGender == genderless)
            {
                Gender.Text = "Genderless";
            }
            else if (RGender == half)
            {
                Gender.Text = "50/50";
            }
            else if (RGender == "127")
            {
                Gender.Text = "75% Female";
            }
            else
            {
                Gender.Text = RGender;
            }
            //Hatch Rate
            br.BaseStream.Seek(Offset + 17, SeekOrigin.Begin);
            Hatch.Text = "Hatch Rate: " + Convert.ToString(br.ReadByte());
            //Base Happiness
            br.BaseStream.Seek(Offset + 18, SeekOrigin.Begin);
            Happy.Text = Convert.ToString(br.ReadByte());
            //Exp growth Rate
            br.BaseStream.Seek(Offset + 19, SeekOrigin.Begin);
            string EXPRate = Convert.ToString(br.ReadByte());
            Rate.Text = GetEXPRate(EXPRate);
            //Egg groups
            br.BaseStream.Seek(Offset + 20, SeekOrigin.Begin);
            string Egg1 = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 21, SeekOrigin.Begin);
            string Egg2 = Convert.ToString(br.ReadByte());
            //Obtains correct labeling for egg groups
            REgg1.Text = GetEggGroup(Egg1);
            REgg2.Text = GetEggGroup(Egg2);
            //Abilities, idk why but ability 2 doesnt always work
            br.BaseStream.Seek(Offset + 22, SeekOrigin.Begin);
            Abil1.Text = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 23, SeekOrigin.Begin);
            Abil2.Text = Convert.ToString(br.ReadByte());
            //Safari Zone run rate
            br.BaseStream.Seek(Offset + 24, SeekOrigin.Begin);
            Safari.Text = "Safari Zone Rate: " + Convert.ToString(br.ReadByte());
            //Color
            br.BaseStream.Seek(Offset + 25, SeekOrigin.Begin);
            string Cint = Convert.ToString(br.ReadByte());
            Color.Text = "Color: " + GetColor(Cint);
            br.Close();
        }

        //Transfer Stats from ROM to TRF
        private void TransferStats_Click(object sender, EventArgs e)
        {

            //Transfer stats
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(BinFile));
            BinaryReader br = new BinaryReader(File.OpenRead(currentROM));
            uint TRFStat1 = Convert.ToUInt32(TRFPokemonStats);
            uint PokeStat1 = Convert.ToUInt32(PokemonStats);
            long TRFOff = TRFStat1 + (comboBox2.SelectedIndex * 28);
            long PokeOff = PokeStat1 + (comboBox1.SelectedIndex * 28);
            br.BaseStream.Position = PokeOff;
            bw.BaseStream.Position = TRFOff;
            byte[] BytesToWrite = br.ReadBytes(0x1C);
            bw.Write(BytesToWrite);
            bw.Close();
            br.Close();


            //Read Base Stats
            br = new BinaryReader(File.OpenRead(BinFile));
            long Offset = TRFPokemonStats + (comboBox2.SelectedIndex * 0x1C);
            br.BaseStream.Seek(Offset, SeekOrigin.Begin);
            IHp.Text = "HP " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 1, SeekOrigin.Begin);
            IAtk.Text = "ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 2, SeekOrigin.Begin);
            IDef.Text = "DEF " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 3, SeekOrigin.Begin);
            ISAtk.Text = "S.ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 4, SeekOrigin.Begin);
            ISDef.Text = "S.Def " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 5, SeekOrigin.Begin);
            ISpd.Text = "SPD " + Convert.ToString(br.ReadByte());

            //Reads Types
            br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
            byte chunk = br.ReadByte();
            long TypeNames2 = TypeNames + (chunk * 7);
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                IType1.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                IType1.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }
            br.BaseStream.Seek(Offset + 7, SeekOrigin.Begin);
            chunk = br.ReadByte();
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                IType2.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                IType2.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }

            //Catch Rate
            br.BaseStream.Seek(Offset + 8, SeekOrigin.Begin);
            CatchIni.Text = "Catch Rate: " + Convert.ToString(br.ReadByte());
            //EXP Yield
            br.BaseStream.Seek(Offset + 9, SeekOrigin.Begin);
            IYeild.Text = "Yield: " + Convert.ToString(br.ReadByte());
            //Reads Gender and determines what percential it is
            br.BaseStream.Seek(Offset + 16, SeekOrigin.Begin);
            string male = "0";
            string female = "254";
            string genderless = "255";
            string half = "31";

            string Gender = Convert.ToString(br.ReadByte());
            if (Gender == male)
            {
                IGender.Text = "100% Male";
            }
            else if (Gender == female)
            {
                IGender.Text = "100% Female";
            }
            else if (Gender == genderless)
            {
                IGender.Text = "Genderless";
            }
            else if (Gender == half)
            {
                IGender.Text = "50/50";
            }
            else if (Gender == "127")
            {
                IGender.Text = "75% Female";
            }
            else
            { IGender.Text = Gender; }

            //Hatch Rate
            br.BaseStream.Seek(Offset + 17, SeekOrigin.Begin);
            IHatch.Text = "Hatch Rate: " + Convert.ToString(br.ReadByte());
            //Base Happiness
            br.BaseStream.Seek(Offset + 18, SeekOrigin.Begin);
            IHappy.Text = Convert.ToString(br.ReadByte());
            //Exp growth Rate
            br.BaseStream.Seek(Offset + 19, SeekOrigin.Begin);
            string EXPRate = Convert.ToString(br.ReadByte());
            IRate.Text = GetEXPRate(EXPRate);
            //Egg Group
            br.BaseStream.Seek(Offset + 20, SeekOrigin.Begin);
            string Egg1 = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 21, SeekOrigin.Begin);
            string Egg2 = Convert.ToString(br.ReadByte());
            //Obtains correct Egg group name to be displayed
            IEgg1.Text = GetEggGroup(Egg1);
            IEgg2.Text = GetEggGroup(Egg2);
            //Abilities
            br.BaseStream.Seek(Offset + 22, SeekOrigin.Begin);
            IAbil1.Text = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 23, SeekOrigin.Begin);
            IAbil2.Text = Convert.ToString(br.ReadByte());
            //Safari Zone Run Rate
            br.BaseStream.Seek(Offset + 24, SeekOrigin.Begin);
            ISafari.Text = "Safari Zone Rate: " + Convert.ToString(br.ReadByte());
            //Color
            br.BaseStream.Seek(Offset + 25, SeekOrigin.Begin);
            string Cint = Convert.ToString(br.ReadByte());
            IColor.Text = "Color: " + GetColor(Cint);
            //Item Reading is broken and removed, will fix in the future
            br.Close();
        }

        //Transfer Name from ROM to TRF
        private void TransferName_Click(object sender, EventArgs e)
        {
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(BinFile));
            BinaryReader br = new BinaryReader(File.OpenRead(currentROM));
            //Transfer name
            uint TRFStat1 = Convert.ToUInt32(TRFpokemonNamesLocation);
            uint PokeStat1 = Convert.ToUInt32(pokemonNamesLocation);
            long TRFOff = TRFStat1 + (comboBox2.SelectedIndex * 11);
            long PokeOff = PokeStat1 + (comboBox1.SelectedIndex * 11);
            bw.BaseStream.Position = TRFOff;
            br.BaseStream.Position = PokeOff;
            Byte[] BytesToWrite = br.ReadBytes(0xB);
            bw.Write(BytesToWrite);
            int test = comboBox2.SelectedIndex;
            br.Close();
            bw.Close();
            //Reloads Names
            comboBox2.Items.Clear();
            for (uint i = 0; i <= TRFnumberOfPokemon; i++)
            {
                if (i <= TRFnumberOfPokemon)
                    comboBox2.Items.Add(BinCharactersToString(10, (uint)(0xB * i + TRFpokemonNamesLocation)));
            }
            comboBox2.SelectedIndex = test;

        }

        //Transfer Both Name and stats while moving to next Pokemon
        private void Next_Click(object sender, EventArgs e)
        {
            //Transfer stats
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(BinFile));
            BinaryReader br = new BinaryReader(File.OpenRead(currentROM));
            uint TRFStat1 = Convert.ToUInt32(TRFPokemonStats);
            uint PokeStat1 = Convert.ToUInt32(PokemonStats);
            long TRFOff = TRFStat1 + (comboBox2.SelectedIndex * 28);
            long PokeOff = PokeStat1 + (comboBox1.SelectedIndex * 28);
            br.BaseStream.Position = PokeOff;
            bw.BaseStream.Position = TRFOff;
            byte[] BytesToWrite = br.ReadBytes(0x1C);
            bw.Write(BytesToWrite);
            //Transfer name
            TRFStat1 = Convert.ToUInt32(TRFpokemonNamesLocation);
            PokeStat1 = Convert.ToUInt32(pokemonNamesLocation);
            TRFOff = TRFStat1 + (comboBox2.SelectedIndex * 11);
            PokeOff = PokeStat1 + (comboBox1.SelectedIndex * 11);
            bw.BaseStream.Position = TRFOff;
            br.BaseStream.Position = PokeOff;
            BytesToWrite = br.ReadBytes(0xB);
            bw.Write(BytesToWrite);
            int test = comboBox2.SelectedIndex;
            br.Close();
            bw.Close();
            //Reloads Names
            comboBox2.Items.Clear();
            for (uint i = 0; i <= TRFnumberOfPokemon; i++)
            {
                if (i <= numberOfPokemon)
                    comboBox2.Items.Add(BinCharactersToString(10, (uint)(0xB * i + TRFpokemonNamesLocation)));
            }
            //Next pokemon
            comboBox1.SelectedIndex = (comboBox1.SelectedIndex + 1);
            comboBox2.SelectedIndex = (test + 1);
        }





        //INI Parsing Functions

        //Being Diegoisawesome functions, most of this wouldn't be possible without these functions!
        private void ParseINI(string[] iniFile, string romCode)
        {
            bool getValues = false;
            foreach (string s in iniFile)
            {
                if (s.Equals("[" + romCode + "]"))
                {
                    getValues = true;
                    continue;
                }
                if (getValues)
                {
                    if (s.Equals(@"[/" + romCode + "]"))
                    {
                        break;
                    }
                    else
                    {
                        if (s.StartsWith("GameName"))
                        {
                            gameName = s.Split('=')[1];
                        }
                        if (s.StartsWith("GameType"))
                        {
                            gameType = s.Split('=')[1];
                        }
                        if (s.StartsWith("ItemData"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out itemTable);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out itemTable);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the item names location.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("PokemonStats"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out PokemonStats);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out PokemonStats);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the item names location.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("TypeNames"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out TypeNames);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out TypeNames);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the item names location.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("NumberOfItems"))
                        {
                            bool success = UInt16.TryParse(s.Split('=')[1], out numberOfItems);
                            if (!success)
                            {
                                success = UInt16.TryParse(ToDecimal(s.Split('=')[1]), out numberOfItems);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the number of items.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("NumberofTypes"))
                        {
                            bool success = UInt16.TryParse(s.Split('=')[1], out NumberofTypes);
                            if (!success)
                            {
                                success = UInt16.TryParse(ToDecimal(s.Split('=')[1]), out NumberofTypes);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the number of items.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("PokemonNames"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out pokemonNamesLocation);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out pokemonNamesLocation);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the Pokémon names offset.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("NumberOfPokemon"))
                        {
                            bool success = UInt16.TryParse(s.Split('=')[1], out numberOfPokemon);
                            if (!success)
                            {
                                success = UInt16.TryParse(ToDecimal(s.Split('=')[1]), out numberOfPokemon);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the number of Pokémon.");
                                    break;
                                }

                                if (!getValues)
                                {
                                    gameCode = "Unknown";
                                    gameName = "Unknown ROM";
                                }
                            }
                        }
                    }
                }
            }
        }
        private void ParseTRFINI(string[] iniFile, string romCode)
        {
            bool getValues = false;
            foreach (string s in iniFile)
            {
                if (s.Equals("[" + romCode + "]"))
                {
                    getValues = true;
                    continue;
                }
                if (getValues)
                {
                    if (s.Equals(@"[/" + romCode + "]"))
                    {
                        break;
                    }
                    else
                    {
                        if (s.StartsWith("PokemonStats"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out TRFPokemonStats);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out TRFPokemonStats);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the item names location.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("PokemonNames"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out TRFpokemonNamesLocation);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out TRFpokemonNamesLocation);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the Pokémon names offset.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("NumberOfPokemon"))
                        {
                            bool success = UInt16.TryParse(s.Split('=')[1], out TRFnumberOfPokemon);
                            if (!success)
                            {
                                success = UInt16.TryParse(ToDecimal(s.Split('=')[1]), out TRFnumberOfPokemon);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the number of Pokémon.");
                                    break;
                                }

                                if (!getValues)
                                {
                                    gameCode = "Unknown TRF";
                                    gameName = "Unknown TRF";
                                }
                            }
                        }
                    }
                }
            }
        }
        public string ToDecimal(string input)
        {
            if (input.ToLower().StartsWith("0x") || input.ToUpper().StartsWith("&H"))
            {
                return Convert.ToUInt32(input.Substring(2), 16).ToString();
            }
            else if (input.ToLower().StartsWith("0o"))
            {
                return Convert.ToUInt32(input.Substring(2), 8).ToString();
            }
            else if (input.ToLower().StartsWith("0b"))
            {
                return Convert.ToUInt32(input.Substring(2), 2).ToString();
            }
            else if (input.ToLower().StartsWith("0t"))
            {
                return ThornalToDecimal(input.Substring(2));
            }
            else if ((input.StartsWith("[") && input.EndsWith("]")) || (input.StartsWith("{") && input.EndsWith("}")))
            {
                return Convert.ToUInt32(input.Substring(1, (input.Length - 2)), 2).ToString();
            }
            else if (input.ToLower().EndsWith("h"))
            {
                return Convert.ToUInt32(input.Substring(0, (input.Length - 1)), 16).ToString();
            }
            else if (input.ToLower().EndsWith("b"))
            {
                return Convert.ToUInt32(input.Substring(0, (input.Length - 1)), 2).ToString();
            }
            else if (input.ToLower().EndsWith("t"))
            {
                return ThornalToDecimal(input.Substring(0, (input.Length - 1)));
            }
            else if (input.StartsWith("$"))
            {
                return Convert.ToUInt32(input.Substring(1), 16).ToString();
            }
            else
            {
                return Convert.ToUInt32(input, 16).ToString();
            }
        }
        private string ThornalToDecimal(string input)
        {
            uint total = 0;
            char[] temp = input.ToCharArray();
            for (int i = input.Length - 1; i >= 0; i--)
            {
                int value = 0;
                bool success = Int32.TryParse(temp[i].ToString(), out value);
                if (!success)
                {
                    if (temp[i] < 'W' && temp[i] >= 'A')
                    {
                        value = temp[i] - 'A' + 10;
                    }
                    else
                    {
                        throw new FormatException(temp[i] + " is an invalid character in the Base 32 number set.");
                    }
                }
                total += (uint)(Math.Pow((double)32, (double)(input.Length - 1 - i)) * value);
            }
            return total.ToString();
        }
        private string ROMCharactersToString(int maxLength, uint baseLocation)
        {
            string s = "";
            using (BinaryReader br = new BinaryReader(File.OpenRead(currentROM)))
            {
                for (int j = 0; j < maxLength; j++)
                {
                    br.BaseStream.Seek(baseLocation + j, SeekOrigin.Begin);
                    byte textByte = br.ReadByte();
                    if ((textByte != 0xFF))
                    {
                        char temp = ';';
                        bool success = characterValues.TryGetValue(textByte, out temp);
                        s += temp;
                        if (!success)
                        {
                            if (textByte == 0x53)
                            {
                                s = s.Substring(0, s.Length - 1) + "PK";
                            }
                            else if (textByte == 0x54)
                            {
                                s = s.Substring(0, s.Length - 1) + "MN";
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return s;
        }
        private string ROMCharactersToStringAlt(int maxLength, uint baseLocation)
        {
            string s = "";
            using (BinaryReader br = new BinaryReader(File.OpenRead(currentROM)))
            {
                for (int j = 0; j < maxLength; j++)
                {
                    br.BaseStream.Seek(baseLocation + j, SeekOrigin.Begin);
                    byte textByte = br.ReadByte();
                    if ((textByte != 0xFF))
                    {
                        char temp = ';';
                        bool success = characterValuesalt.TryGetValue(textByte, out temp);
                        s += temp;
                        if (!success)
                        {
                            if (textByte == 0x53)
                            {
                                s = s.Substring(0, s.Length - 1) + "PK";
                            }
                            else if (textByte == 0x54)
                            {
                                s = s.Substring(0, s.Length - 1) + "MN";
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return s;
        }
        private string BinCharactersToString(int maxLength, long baseLocation)
        {
            string s = "";
            using (BinaryReader br = new BinaryReader(File.OpenRead(BinFile)))
            {
                for (int j = 0; j < maxLength; j++)
                {
                    br.BaseStream.Seek(baseLocation + j, SeekOrigin.Begin);
                    byte textByte = br.ReadByte();
                    if ((textByte != 0xFF))
                    {
                        char temp = ';';
                        bool success = characterValues.TryGetValue(textByte, out temp);
                        s += temp;
                        if (!success)
                        {

                            if (textByte == 0x53)
                            {
                                s = s.Substring(0, s.Length - 1) + "PK";
                            }
                            else if (textByte == 0x54)
                            {
                                s = s.Substring(0, s.Length - 1) + "MN";
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return s;
        }
        private Dictionary<byte, char> ReadTableFile(string iniLocation)
        {
            Dictionary<byte, char> characterValues = new Dictionary<byte, char>();
            string[] tableFile = System.IO.File.ReadAllLines(iniLocation);
            int index = 0;
            foreach (string s in tableFile)
            {
                if (!s.Equals("") && !s.Equals("[Table]") && index != 0x9E && index != 0x9F)
                {
                    string[] stuff = s.Split('=');
                    switch (Byte.Parse(ToDecimal("0x" + stuff[0])))
                    {
                        case 0:
                            characterValues.Add(0, ' ');
                            break;
                        case 0x34:
                            break;
                        case 0x35:
                            characterValues.Add(0x35, '=');
                            break;
                        case 0x53:
                            break;
                        case 0x54:
                            break;
                        case 0x55:
                            break;
                        case 0x56:
                            break;
                        case 0x57:
                            break;
                        case 0x58:
                            break;
                        case 0x59:
                            break;
                        case 0x79:
                            break;
                        case 0x7A:
                            break;
                        case 0x7B:
                            break;
                        case 0x7C:
                            break;
                        case 0xB0:
                            break;
                        case 0xEF:
                            break;
                        case 0xF7:
                            break;
                        case 0xF8:
                            break;
                        case 0xF9:
                            break;
                        case 0xFA:
                            break;
                        case 0xFB:
                            break;
                        case 0xFC:
                            break;
                        case 0xFD:
                            break;
                        case 0xFE:
                            break;
                        case 0xFF:
                            break;
                        default:
                            characterValues.Add(Byte.Parse(ToDecimal("0x" + stuff[0])), stuff[1].ToCharArray()[0]);
                            break;
                    }
                    index++;
                }
            }
            return characterValues;
        }

        public void WriteText(string input, string offset)
        {
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(BinFile));
            StringBuilder description = new StringBuilder(input);
            for (int i = 0; i < description.Length; i++)
            {
                # region letters
                switch (description[i])
                {
                    case ' ':
                        {
                            bw.Write(0x00);
                            break;
                        };
                    case 'À':
                        {
                            bw.Write(0x01);
                            break;
                        };
                    case 'Á':
                        {
                            bw.Write(0x02);
                            break;
                        };
                    case 'Â':
                        {
                            bw.Write(0x03);
                            break;
                        };
                    case 'Ç':
                        {
                            bw.Write(0x04);
                            break;
                        };
                    case 'È':
                        {
                            bw.Write(0x05);
                            break;
                        };
                    case 'É':
                        {
                            bw.Write(0x06);
                            break;
                        };
                    case 'Ê':
                        {
                            bw.Write(0x07);
                            break;
                        };
                    case 'Ë':
                        {
                            bw.Write(0x08);
                            break;
                        };
                    case 'Ì':
                        {
                            bw.Write(0x09);
                            break;
                        };
                    case 'Î':
                        {
                            bw.Write(0x0B);
                            break;
                        };
                    case 'Ï':
                        {
                            bw.Write(0x0C);
                            break;
                        };
                    case 'Ò':
                        {
                            bw.Write(0x0D);
                            break;
                        };
                    case 'Ó':
                        {
                            bw.Write(0x0E);
                            break;
                        };
                    case 'Ô':
                        {
                            bw.Write(0x0F);
                            break;
                        };
                    case 'Œ':
                        {
                            bw.Write(0x10);
                            break;
                        };
                    case 'Ù':
                        {
                            bw.Write(0x11);
                            break;
                        };
                    case 'Ú':
                        {
                            bw.Write(0x12);
                            break;
                        };
                    case 'Û':
                        {
                            bw.Write(0x13);
                            break;
                        };
                    case 'Ñ':
                        {
                            bw.Write(0x14);
                            break;
                        };
                    case 'ß':
                        {
                            bw.Write(0x15);
                            break;
                        };
                    case 'à':
                        {
                            bw.Write(0x16);
                            break;
                        };
                    case 'á':
                        {
                            bw.Write(0x17);
                            break;
                        };
                    case 'ç':
                        {
                            bw.Write(0x19);
                            break;
                        };
                    case 'è':
                        {
                            bw.Write(0x1A);
                            break;
                        };
                    case 'é':
                        {
                            bw.Write(0x1B);
                            break;
                        };
                    case 'ê':
                        {
                            bw.Write(0x1C);
                            break;
                        };
                    case 'ë':
                        {
                            bw.Write(0x1D);
                            break;
                        };
                    case 'ì':
                        {
                            bw.Write(0x1E);
                            break;
                        };
                    case 'î':
                        {
                            bw.Write(0x20);
                            break;
                        };
                    case 'ï':
                        {
                            bw.Write(0x21);
                            break;
                        };
                    case 'ò':
                        {
                            bw.Write(0x22);
                            break;
                        };
                    case 'ó':
                        {
                            bw.Write(0x23);
                            break;
                        };
                    case 'ô':
                        {
                            bw.Write(0x24);
                            break;
                        };
                    case 'œ':
                        {
                            bw.Write(0x25);
                            break;
                        };
                    case 'ù':
                        {
                            bw.Write(0x26);
                            break;
                        };
                    case 'ú':
                        {
                            bw.Write(0x27);
                            break;
                        };
                    case 'û':
                        {
                            bw.Write(0x28);
                            break;
                        };
                    case 'ñ':
                        {
                            bw.Write(0x29);
                            break;
                        };
                    case 'º':
                        {
                            bw.Write(0x2A);
                            break;
                        };
                    case 'ª':
                        {
                            bw.Write(0x2B);
                            break;
                        };
                    case '&':
                        {
                            bw.Write(0x2D);
                            break;
                        };
                    case '+':
                        {
                            bw.Write(0x2E);
                            break;
                        };
                    case '=':
                        {
                            bw.Write(0x35);
                            break;
                        };
                    case ';':
                        {
                            bw.Write(0x36);
                            break;
                        };
                    case '¿':
                        {
                            bw.Write(0x51);
                            break;
                        };
                    case '¡':
                        {
                            bw.Write(0x52);
                            break;
                        };
                    case 'Í':
                        {
                            bw.Write(0x5A);
                            break;
                        };
                    case '%':
                        {
                            bw.Write(0x5B);
                            break;
                        };
                    case '(':
                        {
                            bw.Write(0x5C);
                            break;
                        };
                    case ')':
                        {
                            bw.Write(0x5D);
                            break;
                        };
                    case 'â':
                        {
                            bw.Write(0x68);
                            break;
                        };
                    case 'í':
                        {
                            bw.Write(0x6F);
                            break;
                        };
                    case '<':
                        {
                            bw.Write(0x85);
                            break;
                        };
                    case '>':
                        {
                            bw.Write(0x86);
                            break;
                        };
                    case '0':
                        {
                            bw.Write(0xA1);
                            break;
                        };
                    case '1':
                        {
                            bw.Write(0xA2);
                            break;
                        };
                    case '2':
                        {
                            bw.Write(0xA3);
                            break;
                        };
                    case '3':
                        {
                            bw.Write(0xA4);
                            break;
                        };
                    case '4':
                        {
                            bw.Write(0xA5);
                            break;
                        };
                    case '5':
                        {
                            bw.Write(0xA6);
                            break;
                        };
                    case '6':
                        {
                            bw.Write(0xA7);
                            break;
                        };
                    case '7':
                        {
                            bw.Write(0xA8);
                            break;
                        };
                    case '8':
                        {
                            bw.Write(0xA9);
                            break;
                        };
                    case '9':
                        {
                            bw.Write(0x09);
                            break;
                        };
                    case '!':
                        {
                            bw.Write(0xAB);
                            break;
                        };
                    case '?':
                        {
                            bw.Write(0xAC);
                            break;
                        };
                    case '.':
                        {
                            bw.Write(0xAD);
                            break;
                        };
                    case '-':
                        {
                            bw.Write(0xAE);
                            break;
                        };
                    case '·':
                        {
                            bw.Write(0xAF);
                            break;
                        };
                    case '"':
                        {
                            bw.Write(0xB2);
                            break;
                        };
                    case '\'':
                        {
                            bw.Write(0xB4);
                            break;
                        };
                    case ',':
                        {
                            bw.Write(0xB8);
                            break;
                        };
                    case '/':
                        {
                            bw.Write(0xBA);
                            break;
                        };
                    case 'A':
                        {
                            bw.Write(0xBB);
                            break;
                        };
                    case 'B':
                        {
                            bw.Write(0xBC);
                            break;
                        };
                    case 'C':
                        {
                            bw.Write(0xBD);
                            break;
                        };
                    case 'D':
                        {
                            bw.Write(0xBE);
                            break;
                        };
                    case 'E':
                        {
                            bw.Write(0xBF);
                            break;
                        };
                    case 'F':
                        {
                            bw.Write(0xC0);
                            break;
                        };
                    case 'G':
                        {
                            bw.Write(0xC1);
                            break;
                        };
                    case 'H':
                        {
                            bw.Write(0xC2);
                            break;
                        };
                    case 'I':
                        {
                            bw.Write(0xC3);
                            break;
                        };
                    case 'J':
                        {
                            bw.Write(0xC4);
                            break;
                        };
                    case 'K':
                        {
                            bw.Write(0xC5);
                            break;
                        };
                    case 'L':
                        {
                            bw.Write(0xC6);
                            break;
                        };
                    case 'M':
                        {
                            bw.Write(0xC7);
                            break;
                        };
                    case 'N':
                        {
                            bw.Write(0xC8);
                            break;
                        };
                    case 'O':
                        {
                            bw.Write(0xC9);
                            break;
                        };
                    case 'P':
                        {
                            bw.Write(0xCA);
                            break;
                        };
                    case 'Q':
                        {
                            bw.Write(0xCB);
                            break;
                        };
                    case 'R':
                        {
                            bw.Write(0xCC);
                            break;
                        };
                    case 'S':
                        {
                            bw.Write(0xCD);
                            break;
                        };
                    case 'T':
                        {
                            bw.Write(0xCE);
                            break;
                        };
                    case 'U':
                        {
                            bw.Write(0xCF);
                            break;
                        };
                    case 'V':
                        {
                            bw.Write(0xD0);
                            break;
                        };
                    case 'W':
                        {
                            bw.Write(0xD1);
                            break;
                        };
                    case 'X':
                        {
                            bw.Write(0xD2);
                            break;
                        };
                    case 'Y':
                        {
                            bw.Write(0xD3);
                            break;
                        };
                    case 'Z':
                        {
                            bw.Write(0xD4);
                            break;
                        };
                    case 'a':
                        {
                            bw.Write(0xD5);
                            break;
                        };
                    case 'b':
                        {
                            bw.Write(0xD6);
                            break;
                        };
                    case 'c':
                        {
                            bw.Write(0xD7);
                            break;
                        };
                    case 'd':
                        {
                            bw.Write(0xD8);
                            break;
                        };
                    case 'e':
                        {
                            bw.Write(0xD9);
                            break;
                        };
                    case 'f':
                        {
                            bw.Write(0xDA);
                            break;
                        };
                    case 'g':
                        {
                            bw.Write(0xDB);
                            break;
                        };
                    case 'h':
                        {
                            bw.Write(0xDC);
                            break;
                        };
                    case 'i':
                        {
                            bw.Write(0xDD);
                            break;
                        };
                    case 'j':
                        {
                            bw.Write(0xDE);
                            break;
                        };
                    case 'k':
                        {
                            bw.Write(0xDF);
                            break;
                        };
                    case 'l':
                        {
                            bw.Write(0xE0);
                            break;
                        };
                    case 'm':
                        {
                            bw.Write(0xE1);
                            break;
                        };
                    case 'n':
                        {
                            bw.Write(0xE2);
                            break;
                        };
                    case 'o':
                        {
                            bw.Write(0xE3);
                            break;
                        };
                    case 'p':
                        {
                            bw.Write(0xE4);
                            break;
                        };
                    case 'q':
                        {
                            bw.Write(0xE5);
                            break;
                        };
                    case 'r':
                        {
                            bw.Write(0xE6);
                            break;
                        };
                    case 's':
                        {
                            bw.Write(0xE7);
                            break;
                        };
                    case 't':
                        {
                            bw.Write(0xE8);
                            break;
                        };
                    case 'u':
                        {
                            bw.Write(0xE9);
                            break;
                        };
                    case 'v':
                        {
                            bw.Write(0xEA);
                            break;
                        };
                    case 'w':
                        {
                            bw.Write(0xEB);
                            break;
                        };
                    case 'x':
                        {
                            bw.Write(0xEC);
                            break;
                        };
                    case 'y':
                        {
                            bw.Write(0xED);
                            break;
                        };
                    case 'z':
                        {
                            bw.Write(0xEE);
                            break;
                        };
                    case ':':
                        {
                            bw.Write(0xF0);
                            break;
                        };
                    case 'Ä':
                        {
                            bw.Write(0xF1);
                            break;
                        };
                    case 'Ö':
                        {
                            bw.Write(0xF2);
                            break;
                        };
                    case 'Ü':
                        {
                            bw.Write(0xF3);
                            break;
                        };
                    case 'ä':
                        {
                            bw.Write(0xF4);
                            break;
                        };
                    case 'ö':
                        {
                            bw.Write(0xF5);
                            break;
                        };
                    case 'ü':
                        {
                            bw.Write(0xF6);
                            break;
                        };
                }
                if (description[i] == '\\' && i < description.Length - 1)
                {
                    switch (description[i + 1])
                    {
                        case 'e':
                            {
                                bw.Write(0x1B);
                                description.Remove(i, 1);
                                break;
                            };
                        case 'l':
                            {
                                bw.Write(0xFA);
                                description.Remove(i, 1);
                                break;
                            };
                        case 'p':
                            {
                                bw.Write(0xFB);
                                description.Remove(i, 1);
                                break;
                            };
                        case 'c':
                            {
                                bw.Write(0xFC);
                                description.Remove(i, 1);
                                break;
                            };
                        case 'v':
                            {
                                bw.Write(0xFD);
                                description.Remove(i, 1);
                                break;
                            };
                        case 'n':
                            {
                                bw.Write(0xFE);
                                description.Remove(i, 1);
                                break;
                            };
                        case 'x':
                            {
                                bw.Write(0xFF);
                                description.Remove(i, 1);
                                break;
                            };
                        default:
                            {
                                break;
                            };
                    }
                }
                else if (description[i] == '[' && i < description.Length - 2)
                {
                    string block = "";
                    int n = 1;
                    if (i + n < description.Length)
                    {
                        while (description[i + n] != ']' && n < 4 && i + n < description.Length - 1)
                        {
                            block += description[i + n];
                            n++;
                        }

                        switch (block)
                        {
                            case "Lv":
                                {
                                    bw.Write(0x34);
                                    description.Remove(i + 1, 3);
                                    break;
                                }
                            case "pk":
                                {
                                    bw.Write(0x53);
                                    description.Remove(i + 1, 3);
                                    break;
                                }
                            case "mn":
                                {
                                    bw.Write(0x54);
                                    description.Remove(i + 1, 3);
                                    break;
                                }
                            case "po":
                                {
                                    bw.Write(0x55);
                                    description.Remove(i + 1, 3);
                                    break;
                                }
                            case "ké":
                                {
                                    bw.Write(0x56);
                                    description.Remove(i + 1, 3);
                                    break;
                                }
                            case "bl":
                                {
                                    bw.Write(0x57);
                                    description.Remove(i + 1, 3);
                                    break;
                                }
                            case "oc":
                                {
                                    bw.Write(0x58);
                                    description.Remove(i + 1, 3);
                                    break;
                                }
                            case "k":
                                {
                                    bw.Write(0x59);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "U":
                                {
                                    bw.Write(0x79);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "D":
                                {
                                    bw.Write(0x7A);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "L":
                                {
                                    bw.Write(0x7B);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "R":
                                {
                                    bw.Write(0x7C);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case ".":
                                {
                                    bw.Write(0xB0);
                                    description.Remove(i + 1, 2);
                                    break;
                                }

                            case "\"":
                                {
                                    bw.Write(0xB1);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "'":
                                {
                                    bw.Write(0xB3);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "m":
                                {
                                    bw.Write(0xB5);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "f":
                                {
                                    bw.Write(0xB6);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "$":
                                {
                                    bw.Write(0xB7);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "x":
                                {
                                    bw.Write(0xB9);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case ">":
                                {
                                    bw.Write(0xEF);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "u":
                                {
                                    bw.Write(0xF7);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "d":
                                {
                                    bw.Write(0xF8);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            case "l":
                                {
                                    bw.Write(0xF9);
                                    description.Remove(i + 1, 2);
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }
                #endregion

            }
            bw.Write(0xFF);
            bw.Close();
        }
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length / 2;
            byte[] bytes = new byte[NumberChars];
            using (var sr = new StringReader(hex))
            {
                for (int i = 0; i < NumberChars; i++)
                    bytes[i] =
                      Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
            }
            return bytes;
        }
        //End of Diegoisawesome credits


        public string GetEggGroup(string Egg)
        {
            if (Egg == "1")
            {
                Egg = "Monster";
            }
            else if (Egg == "2")
            {
                Egg = "Water 1";
            }
            else if (Egg == "3")
            {
                Egg = "Bug";
            }
            else if (Egg == "4")
            {
                Egg = "Flying";
            }
            else if (Egg == "5")
            {
                Egg = "Field";
            }
            else if (Egg == "6")
            {
                Egg = "Fairy";
            }
            else if (Egg == "7")
            {
                Egg = "Grass";
            }
            else if (Egg == "8")
            {
                Egg = "Human-Like";
            }
            else if (Egg == "9")
            {
                Egg = "Water 3";
            }
            else if (Egg == "10")
            {
                Egg = "Mineral";
            }
            else if (Egg == "11")
            {
                Egg = "Amorphous";
            }
            else if (Egg == "12")
            {
                Egg = "Water 2";
            }
            else if (Egg == "13")
            {
                Egg = "Ditto";
            }
            else if (Egg == "14")
            {
                Egg = "Dragon";
            }
            else if (Egg == "15")
            {
                Egg = "Undiscovered";
            }
            return Egg;
        }
        public string GetEXPRate(string Exp)
        {
            if (Exp == "0")
            {
                Exp = "Medium Fast";
            }
            else if (Exp == "1")
            {
                Exp = "Erratic";
            }
            else if (Exp == "2")
            {
                Exp = "Fluctuating";
            }
            else if (Exp == "3")
            {
                Exp = "Medium Slow";
            }
            else if (Exp == "4")
            {
                Exp = "Fast";
            }
            else if (Exp == "5")
            {
                Exp = "Slow";
            }
            return Exp;
        }
        public string GetColor(string Color)
        {
            if (Color == "0")
            {
                Color = "Red";
            }
            else if (Color == "1")
            {
                Color = "Blue";
            }
            else if (Color == "2")
            {
                Color = "Yellow";
            }
            else if (Color == "3")
            {
                Color = "Green";
            }
            else if (Color == "4")
            {
                Color = "Black";
            }
            else if (Color == "5")
            {
                Color = "Brown";
            }
            else if (Color == "6")
            {
                Color = "Purple";
            }
            else if (Color == "7")
            {
                Color = "Gray";
            }
            else if (Color == "8")
            {
                Color = "White";
            }
            else if (Color == "9")
            {
                Color = "Pink";
            }
            return Color;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            progressBar1.Visible = true;
            int ROMPoke = 0;
            int TRFPoke = 0;
            progressBar1.Maximum = TRFnumberOfPokemon;
            for (uint i = 0; i <= numberOfPokemon; i++)
            {
                BinaryWriter bw = new BinaryWriter(File.OpenWrite(BinFile));
                BinaryReader br = new BinaryReader(File.OpenRead(currentROM));
                uint vOut1 = Convert.ToUInt32(TRFPokemonStats);
                uint vOut2 = Convert.ToUInt32(PokemonStats);
                long Offset2 = vOut1 + (TRFPoke * 28);
                long Offset = vOut2 + (ROMPoke * 28);
                br.BaseStream.Position = Offset;
                bw.BaseStream.Position = Offset2;
                byte[] BytesToWrite = br.ReadBytes(0x1C);
                bw.Write(BytesToWrite);
                //Transfer name
                vOut1 = Convert.ToUInt32(TRFpokemonNamesLocation);
                vOut2 = Convert.ToUInt32(pokemonNamesLocation);
                Offset2 = vOut1 + (TRFPoke * 11);
                Offset = vOut2 + (ROMPoke * 11);
                br.BaseStream.Position = Offset;
                bw.BaseStream.Position = Offset2;
                BytesToWrite = br.ReadBytes(0xB);
                bw.Write(BytesToWrite);
                br.Close();
                bw.Close();
                //Next pokemon
                ROMPoke = ROMPoke + 1;
                TRFPoke = TRFPoke + 1;
                progressBar1.PerformStep();
            }
            //Reloads Names
            comboBox2.Items.Clear();
            for (uint ai = 0; ai <= TRFnumberOfPokemon; ai++)
            {
                if (ai <= 0x1FE)
                    comboBox2.Items.Add(BinCharactersToString(10, (uint)(0xB * ai + TRFpokemonNamesLocation)));
            }
            progressBar1.Visible = false;
            progressBar1.Value = 0;
        }

        private void zeroOutTRFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(BinFile));
            uint vOut1 = Convert.ToUInt32(TRFPokemonStats);
            uint vOut2 = Convert.ToUInt32(TRFpokemonNamesLocation);
            int size = (TRFnumberOfPokemon * 0x1C);
            int size2 = (TRFnumberOfPokemon * 0xB);
            byte[] Stats = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] Name = { 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xFF };
            for (uint i = 0; i <= TRFnumberOfPokemon; i++)
            {
                long Offset = vOut1 + (i * 0x1C);
                bw.BaseStream.Position = Offset;
                bw.Write(Stats);

                long Offset2 = vOut2 + (i * 0xB);
                bw.BaseStream.Position = Offset2;
                bw.Write(Name);
            }
            bw.Close();
            //Reloads Names
            comboBox2.Items.Clear();
            for (uint ai = 0; ai <= TRFnumberOfPokemon; ai++)
            {
                if (ai <= 0x1FE)
                    comboBox2.Items.Add(BinCharactersToString(10, (uint)(0xB * ai + TRFpokemonNamesLocation)));
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string path = System.Windows.Forms.Application.StartupPath + @"\Export\";

            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(path))
                {
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);

            }
            catch
            {

            }
            finally
            {
                string Name = comboBox1.SelectedItem.ToString();
                string fileName = System.Windows.Forms.Application.StartupPath + @"\Export\" + Name + ".pkmn";
                using (BinaryWriter bw = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    BinaryReader br = new BinaryReader(File.OpenRead(currentROM));
                    uint vOut2 = Convert.ToUInt32(PokemonStats);
                    long Offset = vOut2 + (comboBox1.SelectedIndex * 28);
                    br.BaseStream.Position = Offset;
                    byte[] BytesToWrite = br.ReadBytes(0x1C);
                    bw.Write(BytesToWrite);
                    br.Close();
                    bw.Close();
                }
            }
        }

        public string Translate(string input)
        {
            StringBuilder conversion = new StringBuilder(input);
            for (int i = 0; i < conversion.Length; i++)
            {
                if (conversion[i] == '\\' && i < conversion.Length - 1)
                {
                    switch (conversion[i + 1])
                    {
                        case 'n':
                            {
                                conversion[i + 1] = (char)10;
                                conversion.Remove(i, 1);
                                break;
                            };
                        case 'l':
                            {
                                conversion[i + 1] = (char)10;
                                conversion.Remove(i, 1);
                                break;
                            };
                        case 'e':
                            {
                                conversion[i + 1] = 'é';
                                conversion.Remove(i, 1);
                                break;
                            };
                        default:
                            {
                                break;
                            };
                    }
                }
                else if (conversion[i] == '[' && i < conversion.Length - 2)
                {
                    string block = "";
                    int n = 1;
                    if (i + n < conversion.Length)
                    {
                        while (conversion[i + n] != ']' && n < 4 && i + n < conversion.Length - 1)
                        {
                            block += conversion[i + n];
                            n++;
                        }

                        switch (block)
                        {
                            case ".":
                                {
                                    conversion[i] = '.';
                                    conversion[i + 1] = '.';
                                    conversion[i + 2] = '.';
                                    break;
                                }
                            case "Lv":
                                {
                                    conversion[i] = 'L';
                                    conversion[i + 1] = 'V';
                                    conversion.Remove(i + 2, 2);
                                    break;
                                }
                            case "pk":
                                {
                                    conversion[i] = 'P';
                                    conversion[i + 1] = 'K';
                                    conversion.Remove(i + 2, 2);
                                    break;
                                }
                            case "mn":
                                {
                                    conversion[i] = 'M';
                                    conversion[i + 1] = 'N';
                                    conversion.Remove(i + 2, 2);
                                    break;
                                }
                            case "po":
                                {
                                    conversion[i] = 'P';
                                    conversion[i + 1] = 'O';
                                    conversion.Remove(i + 2, 2);
                                    break;
                                }

                            case "ké":
                                {
                                    conversion[i] = 'K';
                                    conversion[i + 1] = 'é';
                                    conversion.Remove(i + 2, 2);
                                    break;
                                }
                            case "bl":
                                {
                                    conversion[i] = 'B';
                                    conversion[i + 1] = 'L';
                                    conversion.Remove(i + 2, 2);
                                    break;
                                }
                            case "oc":
                                {
                                    conversion[i] = 'O';
                                    conversion[i + 1] = 'C';
                                    conversion.Remove(i + 2, 2);
                                    break;
                                }
                            case "k":
                                {
                                    conversion[i] = 'K';
                                    conversion.Remove(i + 1, 2);
                                    break;
                                }
                            case "\"\"":
                                {
                                    conversion[i] = '\"';
                                    conversion.Remove(i + 1, 3);
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }


            }

            return conversion.ToString();
        }

        private void createNewTRFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = System.Windows.Forms.Application.StartupPath + @"\Export\";

            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(path))
                {
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);

            }
            catch
            {

            }
            finally
            {
                string CustomTRF = "Custom.TRF";
                string fileName = System.Windows.Forms.Application.StartupPath + @"\Export\" + CustomTRF;
                using (BinaryWriter bw = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    long Offset = 0x0;
                    long OffStats = 0xC0;
                    long OffNames = 0xF000;
                    long OffID = 0xAC;
                    byte[] Stats = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    byte[] Name = { 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xAC, 0xFF };
                    byte[] ID = Encoding.ASCII.GetBytes("CTRF");
                    byte[] Permission = Encoding.ASCII.GetBytes("This is the TRF file for use    with InstaPokeTransfer Tool. Do NOT redistribute without the creator's permission");
                    byte[] Words1 = Encoding.ASCII.GetBytes("Pokemon Stats:");
                    byte[] Words2 = { 0x50, 0x6F, 0x6B, 0x65, 0x6D, 0x6F, 0x6E, 0x20, 0x4E, 0x61, 0x6D, 0x65, 0x73, 0x3A, 0xFF, 0xFF };
                    bw.BaseStream.Position = Offset;
                    bw.Write(Permission);

                    bw.BaseStream.Position = 0xB0;
                    bw.Write(Words1);

                    bw.BaseStream.Position = OffID;
                    bw.Write(ID);


                    for (uint i = 0; i <= 0x88A; i++)
                    {
                        bw.BaseStream.Position = OffStats + (i * 0x1c);
                        if (i <= 0x88A)
                            bw.Write(Stats);
                    }
                    bw.BaseStream.Position = 0xEFF0;
                    bw.Write(Words2);

                    for (uint i = 0; i <= 0x88A; i++)
                    {
                        bw.BaseStream.Position = OffNames + (i * 0xB);
                        if (i <= 0x88A)
                            bw.Write(Name);
                    }


                    bw.Close();
                }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            progressBar1.Maximum = numberOfPokemon;
            progressBar1.Visible = true;
            string path = System.Windows.Forms.Application.StartupPath + @"\Export\";

            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(path))
                {
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);

            }
            catch
            {

            }
            finally
            {
                comboBox1.Items.Clear();
                for (uint ai = 0; ai <= numberOfPokemon; ai++)
                {
                    if (ai <= numberOfPokemon)
                        comboBox1.Items.Add(ROMCharactersToStringAlt(10, (uint)(0xB * ai + pokemonNamesLocation)));
                }
                comboBox1.SelectedIndex = 0;
                for (uint i = 0; i <= numberOfPokemon; i++)
                {
                    string Name = comboBox1.SelectedItem.ToString();
                    string fileName = System.Windows.Forms.Application.StartupPath + @"\Export\" + Name + ".pkmn";
                    BinaryWriter bw = new BinaryWriter(File.Open(fileName, FileMode.Create));
                    BinaryReader br = new BinaryReader(File.OpenRead(currentROM));
                    uint vOut2 = Convert.ToUInt32(PokemonStats - 0x1C);
                    long Offset = vOut2 + (i * 0x1C);
                    br.BaseStream.Position = Offset;
                    byte[] BytesToWrite = br.ReadBytes(0x1C);
                    bw.Write(BytesToWrite);
                    comboBox1.SelectedIndex = Convert.ToInt32(i);
                    br.Close();
                    bw.Close();
                    progressBar1.PerformStep();
                }
                comboBox1.Items.Clear();
                for (uint ai = 0; ai <= numberOfPokemon; ai++)
                {
                    if (ai <= numberOfPokemon)
                        comboBox1.Items.Add(ROMCharactersToString(10, (uint)(0xB * ai + pokemonNamesLocation)));
                }
                progressBar1.Visible = false;
                progressBar1.Value = 0;
            }
        }
        //Help dialogs
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            try
            {
                string fileLoc = System.Windows.Forms.Application.StartupPath + @"\Config\Readme.rtf";
                Process.Start(fileLoc);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show("Could not open Readme.rtf." + ex.Message, "I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            try
            {
                string fileLoc = System.Windows.Forms.Application.StartupPath + @"\Config\ROMS.ini";
                Process.Start(fileLoc);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show("Could not open ROMS.ini. This is a needed file for the program to work!" + ex.Message, "I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            MessageBox.Show("IPTS is a program created by Joexv to make inserting Pokemon so much easier. No longer will you have to manually open a Pokemon Editor to one by one add gen 4 Pokemon. A few clicks with IPTS and you've got yourself all the stats you need to make your game the best game it can be!");
        }
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            MessageBox.Show("A TRF file, or Transfer Reading Filesystem, is a custom made file to work with IPTS without the need of bulky ini files or removing any features. Because of the design of the TRF file, it is 100% future proof. If any new generation of Pokemon are added, you can simply edit them in. That simple!");
        }


        private void button3_Click(object sender, EventArgs e)
        {
            ofd3.Filter = "PKMN file (*.pkmn)|*.pkmn"; //Opens PKMN File
            if (ofd3.ShowDialog() == DialogResult.OK)
            {
                PKMNFile = ofd3.FileName;
                string name = Path.GetFileNameWithoutExtension(ofd3.SafeFileName);
                using (BinaryReader br = new BinaryReader(File.OpenRead(PKMNFile)))
                {
                    BinaryWriter bw = new BinaryWriter(File.OpenWrite(BinFile));
                    uint vOut1 = Convert.ToUInt32(TRFPokemonStats);
                    long Offset2 = vOut1 + (comboBox2.SelectedIndex * 28);
                    br.BaseStream.Position = 0x0;
                    bw.BaseStream.Position = Offset2;
                    byte[] BytesToWrite = br.ReadBytes(0x1C);
                    bw.Write(BytesToWrite);
                    //Transfer name
                    byte[] Zero = { 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, };
                    vOut1 = Convert.ToUInt32(TRFpokemonNamesLocation);
                    Offset2 = vOut1 + (comboBox2.SelectedIndex * 11);
                    byte[] withoutLast = Poketext.Encode(ofd3.SafeFileName.Substring(0, ofd3.SafeFileName.Length - 5));
                    byte[] combined = withoutLast.Concat(Zero).ToArray();
                    bw.BaseStream.Position = Offset2;
                    Array.Resize(ref combined, 11);
                    bw.Write(combined);
                    br.Close();
                    bw.Close();
                    //Reloads Names
                    int Index = comboBox2.SelectedIndex;
                    comboBox2.Items.Clear();
                    for (uint i = 0; i <= TRFnumberOfPokemon; i++)
                    {
                        if (i <= 0x1FE)
                            comboBox2.Items.Add(BinCharactersToString(10, (uint)(0xB * i + TRFpokemonNamesLocation)));
                    }
                    comboBox2.SelectedIndex = Index;
                }
                using (BinaryReader br = new BinaryReader(File.OpenRead(BinFile)))
                {
                    //This entire thing runs fairly slow, reads entire data in about a second, seems to stall more than it should.
                    long Offset = TRFPokemonStats + (comboBox2.SelectedIndex * 0x1C);
                    br.BaseStream.Seek(Offset, SeekOrigin.Begin);
                    IHp.Text = "HP " + Convert.ToString(br.ReadByte());
                    br.BaseStream.Seek(Offset + 1, SeekOrigin.Begin);
                    IAtk.Text = "ATK " + Convert.ToString(br.ReadByte());
                    br.BaseStream.Seek(Offset + 2, SeekOrigin.Begin);
                    IDef.Text = "DEF " + Convert.ToString(br.ReadByte());
                    br.BaseStream.Seek(Offset + 3, SeekOrigin.Begin);
                    ISAtk.Text = "S.ATK " + Convert.ToString(br.ReadByte());
                    br.BaseStream.Seek(Offset + 4, SeekOrigin.Begin);
                    ISDef.Text = "S.Def " + Convert.ToString(br.ReadByte());
                    br.BaseStream.Seek(Offset + 5, SeekOrigin.Begin);
                    ISpd.Text = "SPD " + Convert.ToString(br.ReadByte());

                    //Reads Types
                    br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
                    byte chunk = br.ReadByte();
                    long TypeNames2 = TypeNames + (chunk * 7);
                    //Checks to see if the current ROM supports the needed Type name
                    if (chunk > NumberofTypes)
                    {
                        TypeNames2 = TypeNames + (chunk * 7);
                        IType1.Text = "Unavailable";
                    }
                    else
                    {
                        //Obtains the Type Names from ROM
                        TypeNames2 = TypeNames + (chunk * 7);
                        IType1.Text = ROMCharactersToString(7, (uint)(TypeNames2));
                    }
                    br.BaseStream.Seek(Offset + 7, SeekOrigin.Begin);
                    chunk = br.ReadByte();
                    //Checks to see if the current ROM supports the needed Type name
                    if (chunk > NumberofTypes)
                    {
                        TypeNames2 = TypeNames + (chunk * 7);
                        IType2.Text = "Unavailable";
                    }
                    else
                    {
                        //Obtains the Type Names from ROM
                        TypeNames2 = TypeNames + (chunk * 7);
                        IType2.Text = ROMCharactersToString(7, (uint)(TypeNames2));
                    }

                    //Catch Rate
                    br.BaseStream.Seek(Offset + 8, SeekOrigin.Begin);
                    CatchIni.Text = "Catch Rate: " + Convert.ToString(br.ReadByte());
                    //EXP Yield
                    br.BaseStream.Seek(Offset + 9, SeekOrigin.Begin);
                    IYeild.Text = "Yield: " + Convert.ToString(br.ReadByte());
                    //Reads Gender and determines what percential it is
                    br.BaseStream.Seek(Offset + 16, SeekOrigin.Begin);
                    string male = "0";
                    string female = "254";
                    string genderless = "255";
                    string half = "31";

                    string Gender = Convert.ToString(br.ReadByte());
                    if (Gender == male)
                    {
                        IGender.Text = "100% Male";
                    }
                    else if (Gender == female)
                    {
                        IGender.Text = "100% Female";
                    }
                    else if (Gender == genderless)
                    {
                        IGender.Text = "Genderless";
                    }
                    else if (Gender == half)
                    {
                        IGender.Text = "50/50";
                    }
                    else if (Gender == "127")
                    {
                        IGender.Text = "75% Female";
                    }
                    else
                    { IGender.Text = Gender; }

                    //Hatch Rate
                    br.BaseStream.Seek(Offset + 17, SeekOrigin.Begin);
                    IHatch.Text = "Hatch Rate: " + Convert.ToString(br.ReadByte());
                    //Base Happiness
                    br.BaseStream.Seek(Offset + 18, SeekOrigin.Begin);
                    IHappy.Text = Convert.ToString(br.ReadByte());
                    //Exp growth Rate
                    br.BaseStream.Seek(Offset + 19, SeekOrigin.Begin);
                    string EXPRate = Convert.ToString(br.ReadByte());
                    IRate.Text = GetEXPRate(EXPRate);
                    //Egg Group
                    br.BaseStream.Seek(Offset + 20, SeekOrigin.Begin);
                    string Egg1 = Convert.ToString(br.ReadByte());
                    br.BaseStream.Seek(Offset + 21, SeekOrigin.Begin);
                    string Egg2 = Convert.ToString(br.ReadByte());
                    //Obtains correct Egg group name to be displayed
                    IEgg1.Text = GetEggGroup(Egg1);
                    IEgg2.Text = GetEggGroup(Egg2);
                    //Abilities
                    br.BaseStream.Seek(Offset + 22, SeekOrigin.Begin);
                    IAbil1.Text = Convert.ToString(br.ReadByte());
                    br.BaseStream.Seek(Offset + 23, SeekOrigin.Begin);
                    IAbil2.Text = Convert.ToString(br.ReadByte());
                    //Safari Zone Run Rate
                    br.BaseStream.Seek(Offset + 24, SeekOrigin.Begin);
                    ISafari.Text = "Safari Zone Rate: " + Convert.ToString(br.ReadByte());
                    //Color
                    br.BaseStream.Seek(Offset + 25, SeekOrigin.Begin);
                    string Cint = Convert.ToString(br.ReadByte());
                    IColor.Text = "Color: " + GetColor(Cint);
                    //Item Reading is broken and removed, will fix in the future
                    br.Close();
                }
            }
        }
    }
}
