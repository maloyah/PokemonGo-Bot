using System.Globalization;
using POGOProtos.Data;
using POGOProtos.Enums;
using POGOProtos.Networking.Responses;
using PokemonGo.RocketAPI.Console.PokeData;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Helpers;
using System;
using System.Threading;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using PokemonGo.RocketAPI.Logic.Utils;
using System.Collections.Generic;
using static PokemonGo.RocketAPI.Console.GUI;
using POGOProtos.Inventory.Item;

namespace PokemonGo.RocketAPI.Console
{
    public partial class Pokemons : Form
    {
        public static string languagestr2;
        private static Client client;
        private static GetPlayerResponse profile;
        private static POGOProtos.Data.Player.PlayerStats stats;
        private static GetInventoryResponse inventory;
        private static IOrderedEnumerable<PokemonData> pokemons;
        private static List<AdditionalPokeData> additionalPokeData = new List<AdditionalPokeData>();

        private void loadAdditionalPokeData()
        {
            try
            {
                var path = "PokeData\\AdditionalPokeData.json";
                var jsonData = File.ReadAllText(path);
                additionalPokeData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AdditionalPokeData>>(jsonData);
            }
            catch (Exception e)
            {
                Logger.ColoredConsoleWrite(ConsoleColor.Red, "Could not load additional PokeData", LogLevel.Error);
            }
        }

        public class taskResponse
        {
            public bool Status { get; set; }
            public string Message { get; set; }
            public taskResponse() { }
            public taskResponse(bool status, string message)
            {
                Status = status;
                Message = message;
            }
        }
        public Pokemons()
        {
            InitializeComponent();
            ClientSettings = new Settings();
            
            InitialzePokemonListView();
            
        }

        public static ISettings ClientSettings;

        private void Pokemons_Load(object sender, EventArgs e)
        {
            loadAdditionalPokeData();
            #region Load GLOBALS for Items change
            //text_MaxPokeballs.Text = Globals.pokeball.ToString();
            //text_MaxGreatBalls.Text =  Globals.greatball.ToString();
            //text_MaxUltraBalls.Text =  Globals.ultraball.ToString();
            //text_MaxRevives.Text = Globals.revive.ToString();
            //text_MaxPotions.Text = Globals.potion.ToString();
            //text_MaxSuperPotions.Text = Globals.superpotion.ToString();
            //text_MaxHyperPotions.Text = Globals.hyperpotion.ToString();
            //text_MaxRazzBerrys.Text = Globals.berry.ToString();
            //text_MaxMasterBalls.Text = Globals.masterball.ToString();
            //text_MaxTopRevives.Text = Globals.toprevive.ToString();
            //text_MaxTopPotions.Text = Globals.toppotion.ToString();
            int count = 0;
            count += Globals.pokeball + Globals.greatball + Globals.ultraball + Globals.revive
                + Globals.potion + Globals.superpotion + Globals.hyperpotion + Globals.berry + Globals.masterball
                + Globals.toprevive + Globals.toppotion;
            text_TotalItemCount.Text = count.ToString();
            #endregion
            reloadsecondstextbox.Text = "60";
            Globals.pauseAtPokeStop = false;
            btnForceUnban.Text = "Pause Walking";
            Execute();
            
        }

        private void Pokemons_Close(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
        }

        public async Task check()
        {
            while (true)
            {
                try
                {
                    if (Logic.Logic._client != null && Logic.Logic._client.readyToUse != false)
                    {
                        break;
                    }
                }
                catch (Exception) { }
            }
        }

        private async void Execute()
        {
            EnabledButton(false, "Reloading Pokemon list.");


            await check();

            try
            {
                client = Logic.Logic._client;
                if (client.readyToUse != false)
                {
                    profile = await client.Player.GetPlayer();
                    await Task.Delay(1000); // Pause to simulate human speed. 
                    inventory = await client.Inventory.GetInventory();
                    pokemons =
                        inventory.InventoryDelta.InventoryItems
                        .Select(i => i.InventoryItemData?.PokemonData)
                            .Where(p => p != null && p?.PokemonId > 0)
                            .OrderByDescending(key => key.Cp);
                    var families = inventory.InventoryDelta.InventoryItems
                        .Select(i => i.InventoryItemData?.Candy)
                        .Where(p => p != null && (int)p?.FamilyId > 0)
                        .OrderByDescending(p => (int)p.FamilyId);

                    var imageSize = 50;

                    var imageList = new ImageList { ImageSize = new Size(imageSize, imageSize) };
                    PokemonListView.SmallImageList = imageList;

                    var templates = await client.Download.GetItemTemplates();
                    var myPokemonSettings = templates.ItemTemplates.Select(i => i.PokemonSettings).Where(p => p != null && p?.FamilyId != PokemonFamilyId.FamilyUnset);
                    var pokemonSettings = myPokemonSettings.ToList();

                    var myPokemonFamilies = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Candy).Where(p => p != null && p?.FamilyId != PokemonFamilyId.FamilyUnset);
                    var pokemonFamilies = myPokemonFamilies.ToArray();


                    

                    PokemonListView.BeginUpdate();
                    foreach (var pokemon in pokemons)
                    {
                        Bitmap pokemonImage = null;
                        await Task.Run(() =>
                        {
                            pokemonImage = GetPokemonLargeImage(pokemon.PokemonId);
                        });
                        imageList.Images.Add(pokemon.PokemonId.ToString(), pokemonImage);

                        PokemonListView.LargeImageList = imageList;
                        var listViewItem = new ListViewItem();
                        listViewItem.Tag = pokemon;



                        var currentCandy = families
                            .Where(i => (int)i.FamilyId <= (int)pokemon.PokemonId)
                            .Select(f => f.Candy_)
                            .First();
                        listViewItem.SubItems.Add(string.Format("{0}", pokemon.Cp));
                        //< listViewItem.SubItems.Add(string.Format("{0}% {1}{2}{3} ({4})", PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0"), pokemon.IndividualAttack.ToString("X"), pokemon.IndividualDefense.ToString("X"), pokemon.IndividualStamina.ToString("X"), (45 - pokemon.IndividualAttack- pokemon.IndividualDefense- pokemon.IndividualStamina) ));
                        listViewItem.SubItems.Add(string.Format("{0}% {1}-{2}-{3}", PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0"), pokemon.IndividualAttack, pokemon.IndividualDefense, pokemon.IndividualStamina));
                        listViewItem.SubItems.Add(string.Format("{0}", PokemonInfo.GetLevel(pokemon)));
                        listViewItem.ImageKey = pokemon.PokemonId.ToString();

                        listViewItem.Text = string.Format((pokemon.Favorite == 1) ? "{0} ★" : "{0}", StringUtils.getPokemonNameByLanguage(ClientSettings, (PokemonId)pokemon.PokemonId));

                        listViewItem.ToolTipText = new DateTime((long)pokemon.CreationTimeMs * 10000).AddYears(1969).ToString("dd/MM/yyyy HH:mm:ss");
                        if (pokemon.Nickname != "")
                            listViewItem.ToolTipText += "\nNickname: " + pokemon.Nickname;

                        var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                        var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);

                        if (settings.EvolutionIds.Count > 0 && familyCandy.Candy_ >= settings.CandyToEvolve)
                        {
                            listViewItem.SubItems.Add("Y (" + familyCandy.Candy_ + "/" + settings.CandyToEvolve + ")");
                            listViewItem.Checked = true;
                        }
                        else
                        {
                            if (settings.EvolutionIds.Count > 0)
                                listViewItem.SubItems.Add("N (" + familyCandy.Candy_ + "/" + settings.CandyToEvolve + ")");
                            else
                                listViewItem.SubItems.Add("N (" + familyCandy.Candy_ + "/Max)");
                        }
                        listViewItem.SubItems.Add(string.Format("{0}", Math.Round(pokemon.HeightM, 2)));
                        listViewItem.SubItems.Add(string.Format("{0}", Math.Round(pokemon.WeightKg, 2)));
                        listViewItem.SubItems.Add(string.Format("{0}/{1}", pokemon.Stamina, pokemon.StaminaMax));
                        listViewItem.SubItems.Add(string.Format("{0}", pokemon.Move1));
                        listViewItem.SubItems.Add(string.Format("{0} ({1})", pokemon.Move2, PokemonInfo.GetAttack(pokemon.Move2)));
                        listViewItem.SubItems.Add(string.Format("{0}", (int)pokemon.PokemonId));
                        listViewItem.SubItems.Add(string.Format("{0}", PokemonInfo.CalculatePokemonPerfectionCP(pokemon).ToString("0.00")));

                        AdditionalPokeData addData = additionalPokeData.FirstOrDefault(x => x.PokedexNumber == (int)pokemon.PokemonId);
                        if (addData != null)
                        {
                            listViewItem.SubItems.Add(addData.Type1);
                            listViewItem.SubItems.Add(addData.Type2);
                        }
                        else
                        {
                            listViewItem.SubItems.Add("");
                            listViewItem.SubItems.Add("");
                        }


                        PokemonListView.Items.Add(listViewItem);
                    }
                    PokemonListView.EndUpdate();
                    PokemonListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    Text = "Pokemon List | User: " + profile.PlayerData.Username + " | Pokemons: " + pokemons.Count() + "/" + profile.PlayerData.MaxPokemonStorage;
                    EnabledButton(true);
                    button2.Enabled = false;
                    checkBox1.Enabled = false;
                    statusTexbox.Text = string.Empty;

                    var arrStats = await client.Inventory.GetPlayerStats();
                    stats = arrStats.First();

                    #region populate fields from settings
                    checkBox_RandomSleepAtCatching.Checked = Globals.sleepatpokemons;
                    checkBox_FarmPokestops.Checked = Globals.farmPokestops;
                    checkBox_CatchPokemon.Checked = Globals.CatchPokemon;
                    checkBox_BreakAtLure.Checked = Globals.BreakAtLure;
                    checkBox_UseLureAtBreak.Checked = Globals.UseLureAtBreak;
                    checkBox_RandomlyReduceSpeed.Checked = Globals.RandomReduceSpeed;
                    checkBox_UseBreakIntervalAndLength.Checked = Globals.UseBreakFields;
                    checkBox_WalkInArchimedeanSpiral.Checked = Globals.Espiral;
                    checkBox_UseGoogleMapsRouting.Checked = Globals.UseGoogleMapsAPI;
                    checkBox10.Checked = Globals.useluckyegg;
                    checkBox9.Checked = Globals.UseAnimationTimes;
                    checkBox2.Checked = Globals.pauseAtEvolve;
                    checkBox7.Checked = Globals.keepPokemonsThatCanEvolve;
                    checkBox6.Checked = Globals.useLuckyEggIfNotRunning;
                    checkBox3.Checked = Globals.userazzberry;
                    checkBox5.Checked = Globals.autoIncubate;
                    checkBox4.Checked = Globals.useBasicIncubators;
                    text_GoogleMapsAPIKey.Text = Globals.GoogleMapsAPIKey;
                    if (File.Exists(Program.items))
                    {
                        string[] lines = File.ReadAllLines(@Program.items);
                        var i = 1;
                        foreach (string line in lines)
                        {
                            switch (i)
                            {
                                case 1:
                                    text_MaxPokeballs.Text = line;
                                    break;
                                case 2:
                                    text_MaxGreatBalls.Text = line;
                                    break;
                                case 3:
                                    text_MaxUltraBalls.Text = line;
                                    break;
                                case 4:
                                    text_MaxRevives.Text = line;
                                    break;
                                case 5:
                                    text_MaxPotions.Text = line;
                                    break;
                                case 6:
                                    text_MaxSuperPotions.Text = line;
                                    break;
                                case 7:
                                    text_MaxHyperPotions.Text = line;
                                    break;
                                case 8:
                                    text_MaxRazzBerrys.Text = line;
                                    break;
                                case 9:
                                    text_MaxMasterBalls.Text = line;
                                    break;
                                case 10:
                                    text_MaxTopPotions.Text = line;
                                    break;
                                case 11:
                                    text_MaxTopRevives.Text = line;
                                    break;
                                default:
                                    break;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        text_MaxPokeballs.Text = "20";
                        text_MaxGreatBalls.Text = "50";
                        text_MaxUltraBalls.Text = "100";
                        text_MaxRevives.Text = "20";
                        text_MaxPotions.Text = "0";
                        text_MaxSuperPotions.Text = "0";
                        text_MaxHyperPotions.Text = "50";
                        text_MaxRazzBerrys.Text = "75";
                        text_MaxMasterBalls.Text = "200";
                        text_MaxTopPotions.Text = "100";
                        text_MaxTopRevives.Text = "20";
                    }
                    //text_MaxPokeballs.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemPokeBall).First().Value);
                    //text_MaxGreatBalls.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemGreatBall).First().Value);
                    //text_MaxUltraBalls.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemUltraBall).First().Value);
                    //text_MaxMasterBalls.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemMasterBall).First().Value);
                    //text_MaxRevives.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemRevive).First().Value);
                    //text_MaxTopRevives.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemMaxRevive).First().Value);
                    //text_MaxPotions.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemPotion).First().Value);
                    //text_MaxSuperPotions.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemSuperPotion).First().Value);
                    //text_MaxHyperPotions.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemHyperPotion).First().Value);
                    //text_MaxTopPotions.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemMaxPotion).First().Value);
                    //text_MaxRazzBerrys.Text = GetRecycleStringValue(_clientSettings.itemRecycleFilter.Where(i => i.Key == ItemId.ItemRazzBerry).First().Value);
                    //textBox2.Text = Globals.razzberry_chance.ToString();
                    #endregion

                    ExecuteItemsLoad();
                }
            }
            catch (Exception e)
            {

                Logger.Error("[PokemonList-Error] " + e.StackTrace);
                await Task.Delay(1000); // Lets the API make a little pause, so we dont get blocked
                Execute();
            }
        }    
        private async void ExecuteItemsLoad()
        {
            try
            {
                client = Logic.Logic._client;
	            if (client.readyToUse != false)
	            {
	               var items = await client.Inventory.GetItems();
	              
	               ItemId[] validsIDs = {ItemId.ItemPokeBall,ItemId.ItemGreatBall,ItemId.ItemUltraBall};
	               
	               ListViewItem listViewItem;
	               foreach (  var item in items) {
	                listViewItem = new ListViewItem();
	                listViewItem.Tag = item;
	                listViewItem.Text = getItemName(item.ItemId);
	                listViewItem.ImageKey = item.ItemId.ToString().Replace("Item","");
	                listViewItem.SubItems.Add(""+item.Count);
	                listViewItem.SubItems.Add(""+item.Unseen);
	                ItemsListView.Items.Add(listViewItem);
	               }
	            }
            }
            catch (Exception e)
            {

                Logger.Error("[ItemsList-Error] " + e.StackTrace);
                await Task.Delay(1000); // Lets the API make a little pause, so we dont get blocked
                ExecuteItemsLoad();
            }
        }
        private string getItemName(ItemId itemID)
        {
            switch (itemID)
            {
                case ItemId.ItemPotion:
                    return "Potion";
                case ItemId.ItemSuperPotion:
                    return "Super Potion";
                case ItemId.ItemHyperPotion:
                    return "Hyper Potion";
                case ItemId.ItemMaxPotion:
                    return "Max Potion";
                case ItemId.ItemRevive:
                    return "Revive";
                case ItemId.ItemIncenseOrdinary:
                    return "Incense";
                case ItemId.ItemPokeBall:
                    return "Poke Ball";
                case ItemId.ItemGreatBall:
                    return "Great Ball";
                case ItemId.ItemUltraBall:
                    return "Ultra Ball";
                case ItemId.ItemMasterBall:
                    return "Master Ball";
                case ItemId.ItemRazzBerry:
                    return "Razz Berry";
                case ItemId.ItemIncubatorBasic:
                    return "Egg Incubator";
                case ItemId.ItemIncubatorBasicUnlimited:
                    return "Unlimited Egg Incubator";
                default:
                    return itemID.ToString().Replace("Item", "");
            }
        }
        async void RecycleToolStripMenuItemClick(object sender, EventArgs e)
        {

            var item = (ItemData)ItemsListView.SelectedItems[0].Tag;
            int amount = IntegerInput.ShowDialog(1, "How many?", item.Count);
            if (amount > 0)
            {
                taskResponse resp = new taskResponse(false, string.Empty);

                resp = await RecycleItems(item, amount);
                if (resp.Status)
                {
                    item.Count -= amount;
                    ItemsListView.SelectedItems[0].SubItems[1].Text = "" + item.Count;
                }
                else
                    MessageBox.Show(resp.Message + " recycle failed!", "Recycle Status", MessageBoxButtons.OK);

            }
        }
        private static async Task<taskResponse> RecycleItems(ItemData item, int amount)
        {
            taskResponse resp1 = new taskResponse(false, string.Empty);
            try
            {
                var resp2 = await client.Inventory.RecycleItem(item.ItemId, amount);

                if (resp2.Result == RecycleInventoryItemResponse.Types.Result.Success)
                {
                    resp1.Status = true;
                }
                else
                {
                    resp1.Message = item.ItemId.ToString();
                }
            }
            catch (Exception e)
            {
                Logger.ColoredConsoleWrite(ConsoleColor.Red, "Error RecycleItem: " + e.Message);
                await RecycleItems(item, amount);
            }
            return resp1;
        }


        private string GetRecycleStringValue(int X)
        {
            return X.ToString();
        }

        private void EnabledButton(bool enabled, string reason = "")
        {
            statusTexbox.Text = reason;
            btnreload.Enabled = enabled;
            btnEvolve.Enabled = enabled;
            btnTransfer.Enabled = enabled;
            btnUpgrade.Enabled = enabled;
            btnFullPowerUp.Enabled = enabled;
            btnShowMap.Enabled = enabled;
            checkBoxreload.Enabled = enabled;
            reloadsecondstextbox.Enabled = enabled;
            PokemonListView.Enabled = enabled;
            btnIVToNick.Enabled = enabled;
            button1.Enabled = enabled;
            button3.Enabled = enabled;
        }

        public static Bitmap GetPokemonSmallImage(PokemonId pokemon)
        {
            return getPokemonImagefromResource(pokemon, "20");
        }

        public static Bitmap GetPokemonMediumImage(PokemonId pokemon)
        {
            return getPokemonImagefromResource(pokemon, "35");
        }

        public static Bitmap GetPokemonLargeImage(PokemonId pokemon)
        {
            return getPokemonImagefromResource(pokemon, "50");
        }

        public static Bitmap GetPokemonVeryLargeImage(PokemonId pokemon)
        {
            return getPokemonImagefromResource(pokemon, "200");
        }

        /// <summary>
        /// Gets the pokemon imagefrom resource.
        /// </summary>
        /// <param name="pokemon">The pokemon.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        private static Bitmap getPokemonImagefromResource(PokemonId pokemon, string size)
        {
            var resource = PokemonGo.RocketAPI.Console.Properties.Resources.ResourceManager.GetObject("_" + (int)pokemon + "_" + size, CultureInfo.CurrentCulture);
            if (resource != null && resource is Bitmap)
            {
                return new Bitmap(resource as Bitmap);
            }
            else
                return null;
        }

        //private static Bitmap GetPokemonImage(int pokemonId)
        //{
        //    var Sprites = AppDomain.CurrentDomain.BaseDirectory + "Sprites\\";
        //    string location = Sprites + pokemonId + ".png";
        //    if (!Directory.Exists(Sprites))
        //        Directory.CreateDirectory(Sprites);
        //    bool err = false;
        //    Bitmap bitmapRemote = null;
        //    if (!File.Exists(location))
        //    {
        //        try
        //        {
        //            ExtendedWebClient wc = new ExtendedWebClient();
        //            wc.DownloadFile("http://pokemon-go.ar1i.xyz/img/pokemons/" + pokemonId + ".png", @location);
        //        }
        //        catch (Exception)
        //        {
        //            // User fail picture
        //            err = true;
        //        }
        //    }
        //    if (err)
        //    {
        //        PictureBox picbox = new PictureBox();
        //        picbox.Image = PokemonGo.RocketAPI.Console.Properties.Resources.error_sprite;
        //        bitmapRemote = (Bitmap)picbox.Image;
        //    }
        //    else
        //    {
        //        try
        //        {
        //            PictureBox picbox = new PictureBox();
        //            FileStream m = new FileStream(location, FileMode.Open);
        //            picbox.Image = Image.FromStream(m);
        //            bitmapRemote = (Bitmap)picbox.Image;
        //            m.Close();
        //        }
        //        catch (Exception e)
        //        {
        //            PictureBox picbox = new PictureBox();
        //            picbox.Image = PokemonGo.RocketAPI.Console.Properties.Resources.error_sprite;
        //            bitmapRemote = (Bitmap)picbox.Image;
        //        }
        //    }
        //    return bitmapRemote;
        //}

        private void btnReload_Click(object sender, EventArgs e)
        {
            PokemonListView.Items.Clear();
            Execute();
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (PokemonListView.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    if (PokemonListView.SelectedItems.Count > 1)
                    {
                        MessageBox.Show("You can only select 1 item for quick action!", "Selection to large", MessageBoxButtons.OK);
                        return;
                    }
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
        }

        private async void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var pokemon = (PokemonData)PokemonListView.SelectedItems[0].Tag;
            taskResponse resp = new taskResponse(false, string.Empty);

            if (MessageBox.Show(this, pokemon.PokemonId + " with " + pokemon.Cp + " CP thats " + Math.Round(PokemonInfo.CalculatePokemonPerfection(pokemon)) + "% perfect", "Are you sure you want to transfer?", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                resp = await transferPokemon(pokemon);
            }
            else
            {
                return;
            }
            if (resp.Status)
            {
                PokemonListView.Items.Remove(PokemonListView.SelectedItems[0]);
                Text = "Pokemon List | User: " + profile.PlayerData.Username + " | Pokemons: " + PokemonListView.Items.Count + "/" + profile.PlayerData.MaxPokemonStorage;
            }
            else
                MessageBox.Show(resp.Message + " transfer failed!", "Transfer Status", MessageBoxButtons.OK);
        }

        private ColumnHeader SortingColumn = null;

        private void PokemonListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ColumnHeader new_sorting_column = PokemonListView.Columns[e.Column];
            System.Windows.Forms.SortOrder sort_order;
            if (SortingColumn == null)
            {
                sort_order = SortOrder.Ascending;
            }
            else
            {
                if (new_sorting_column == SortingColumn)
                {
                    if (SortingColumn.Text.StartsWith("> "))
                    {
                        sort_order = SortOrder.Descending;
                    }
                    else
                    {
                        sort_order = SortOrder.Ascending;
                    }
                }
                else
                {
                    sort_order = SortOrder.Ascending;
                }
                SortingColumn.Text = SortingColumn.Text.Substring(2);
            }

            // Display the new sort order.
            SortingColumn = new_sorting_column;
            if (sort_order == SortOrder.Ascending)
            {
                SortingColumn.Text = "> " + SortingColumn.Text;
            }
            else
            {
                SortingColumn.Text = "< " + SortingColumn.Text;
            }

            // Create a comparer.
            PokemonListView.ListViewItemSorter = new ListViewComparer(e.Column, sort_order);

            // Sort.
            PokemonListView.Sort();
        }

        private async void btnEvolve_Click(object sender, EventArgs e)
        {
            //if (Globals.UseAnimationTimes)
            //{
            //    MessageBox.Show("Staged selected Pokemon for Evolution", "Evolve status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //else
            //{
            EnabledButton(false, "Evolving...");
            var selectedItems = PokemonListView.SelectedItems;
            int evolved = 0;
            int total = selectedItems.Count;
            string failed = string.Empty;
            var date = DateTime.Now.ToString();
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            string evolvelog = System.IO.Path.Combine(logPath, "EvolveLog.txt");

            taskResponse resp = new taskResponse(false, string.Empty);

            if (Globals.pauseAtEvolve2)
            {
                Logger.ColoredConsoleWrite(ConsoleColor.Green, $"Taking a break to evolve some pokemons!");
                Globals.pauseAtWalking = true;
            }


            foreach (ListViewItem selectedItem in selectedItems)
            {
                resp = await evolvePokemon((PokemonData)selectedItem.Tag);

                var pokemoninfo = (PokemonData)selectedItem.Tag;
                var name = pokemoninfo.PokemonId;

                File.AppendAllText(evolvelog, $"[{date}] - MANUAL - Trying to evole Pokemon: {name}" + Environment.NewLine);
                Logger.ColoredConsoleWrite(ConsoleColor.Green, $"Trying to Evolve {name}");

                if (resp.Status)
                {
                    evolved++;
                    statusTexbox.Text = "Evolving..." + evolved;
                }
                else
                    failed += resp.Message + " ";
                if (Globals.UseAnimationTimes)
                {
                    await RandomHelper.RandomDelay(30000, 35000);
                }
                else
                {
                    await RandomHelper.RandomDelay(500, 800);
                }
            }


            if (failed != string.Empty)
            {
                if (_clientSettings.bLogEvolve)
                {
                    File.AppendAllText(evolvelog, $"[{date}] - MANUAL - Sucessfully evolved {evolved}/{total} Pokemons. Failed: {failed}" + Environment.NewLine);
                }
                MessageBox.Show("Succesfully evolved " + evolved + "/" + total + " Pokemons. Failed: " + failed, "Evolve status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            else
            {
                if (_clientSettings.bLogEvolve)
                {
                    File.AppendAllText(evolvelog, $"[{date}] - MANUAL - Sucessfully evolved {evolved}/{total} Pokemons." + Environment.NewLine);
                }
                MessageBox.Show("Succesfully evolved " + evolved + "/" + total + " Pokemons.", "Evolve status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            if (evolved > 0)
            {
                PokemonListView.Items.Clear();
                Execute();
            }
            else
                EnabledButton(true);

            if (Globals.pauseAtEvolve)
            {
                Logger.ColoredConsoleWrite(ConsoleColor.Green, $"Evolved everything. Time to continue our journey!");
                Globals.pauseAtWalking = false;
            }
            //}
        }

        private async void btnTransfer_Click(object sender, EventArgs e)
        {
            //if (Globals.UseAnimationTimes)
            //{
            //    MessageBox.Show("Staged selected Pokemon for Transfer", "Evolve status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //else
            //{
            EnabledButton(false, "Transfering...");
            var selectedItems = PokemonListView.SelectedItems;
            int transfered = 0;
            int total = selectedItems.Count;
            string failed = string.Empty;

            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            string logs = System.IO.Path.Combine(logPath, "TransferLog.txt");
            string date = DateTime.Now.ToString();
            PokemonData pokeData = new PokemonData();


            DialogResult dialogResult = MessageBox.Show("You clicked transfer. This can not be undone.", "Are you Sure?", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                taskResponse resp = new taskResponse(false, string.Empty);

                foreach (ListViewItem selectedItem in selectedItems)
                {
                    resp = await transferPokemon((PokemonData)selectedItem.Tag);
                    if (resp.Status)
                    {
                        var PokemonInfo = (PokemonData)selectedItem.Tag;
                        var name = PokemonInfo.PokemonId;

                        File.AppendAllText(logs, $"[{date}] - MANUAL - Trying to transfer pokemon: {name}" + Environment.NewLine);

                        PokemonListView.Items.Remove(selectedItem);
                        transfered++;
                        statusTexbox.Text = "Transfering..." + transfered;

                    }
                    else
                        failed += resp.Message + " ";
                    await RandomHelper.RandomDelay(5000, 6000);
                }



                if (failed != string.Empty)
                {
                    if (_clientSettings.logManualTransfer)
                    {
                        File.AppendAllText(logs, $"[{date}] - MANUAL - Sucessfully transfered {transfered}/{total} Pokemons. Failed: {failed}" + Environment.NewLine);
                    }
                    MessageBox.Show("Succesfully transfered " + transfered + "/" + total + " Pokemons. Failed: " + failed, "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if (_clientSettings.logManualTransfer)
                    {
                        File.AppendAllText(logs, $"[{date}] - MANUAL - Sucessfully transfered {transfered}/{total} Pokemons." + Environment.NewLine);
                    }
                    MessageBox.Show("Succesfully transfered " + transfered + "/" + total + " Pokemons.", "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                Text = "Pokemon List | User: " + profile.PlayerData.Username + " | Pokemons: " + PokemonListView.Items.Count + "/" + profile.PlayerData.MaxPokemonStorage;
            }
            EnabledButton(true);
            //}
        }

        private async void btnUpgrade_Click(object sender, EventArgs e)
        {
            EnabledButton(false);
            var selectedItems = PokemonListView.SelectedItems;
            int powerdup = 0;
            int total = selectedItems.Count;
            string failed = string.Empty;
            taskResponse resp = new taskResponse(false, string.Empty);

            foreach (ListViewItem selectedItem in selectedItems)
            {
                resp = await PowerUp((PokemonData)selectedItem.Tag);
                if (resp.Status)
                    powerdup++;
                else
                    failed += resp.Message + " ";
                await RandomHelper.RandomDelay(1000, 3000);
            }
            if (failed != string.Empty)
                MessageBox.Show("Succesfully powered up " + powerdup + "/" + total + " Pokemons. Failed: " + failed, "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Succesfully powered up " + powerdup + "/" + total + " Pokemons.", "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (powerdup > 0)
            {
                PokemonListView.Items.Clear();
                Execute();
            }
            else
                EnabledButton(true);
        }

        private async void BtnIVToNickClick(object sender, EventArgs e)
        {
            EnabledButton(false, "Renaming...");
            var selectedItems = PokemonListView.SelectedItems;
            int renamed = 0;
            int total = selectedItems.Count;
            string failed = string.Empty;

            DialogResult dialogResult = MessageBox.Show("You clicked to change nickame using IVs.\nAre you Sure?","Confirm Dialog" , MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                taskResponse resp = new taskResponse(false, string.Empty);

                foreach (ListViewItem selectedItem in selectedItems)
                {
                    PokemonData pokemon = (PokemonData)selectedItem.Tag;
                    pokemon.Nickname = IVsToNickname(pokemon);
                    resp = await changePokemonNickname(pokemon);
                    if (resp.Status)
                    {
                        selectedItem.ToolTipText = new DateTime((long)pokemon.CreationTimeMs * 10000).AddYears(1969).ToString("dd/MM/yyyy HH:mm:ss");
                        selectedItem.ToolTipText += "\nNickname: " + pokemon.Nickname;
                        renamed++;
                        statusTexbox.Text = "Renamig..." + renamed;
                    }
                    else
                        failed += resp.Message + " ";
                    await RandomHelper.RandomDelay(5000, 6000);
                }

                if (failed != string.Empty)
                    MessageBox.Show("Succesfully renamed " + renamed + "/" + total + " Pokemons. Failed: " + failed, "Rename status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("Succesfully renamed " + renamed + "/" + total + " Pokemons.", "Rename status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            EnabledButton(true);
        }

        private static async Task<taskResponse> evolvePokemon(PokemonData pokemon)
        {
            taskResponse resp = new taskResponse(false, string.Empty);
            try
            {
                var evolvePokemonResponse = await client.Inventory.EvolvePokemon((ulong)pokemon.Id);

                if (evolvePokemonResponse.Result == EvolvePokemonResponse.Types.Result.Success)
                {
                    resp.Status = true;
                }
                else
                {
                    resp.Message = pokemon.PokemonId.ToString();
                }

                await RandomHelper.RandomDelay(1000, 2000);
            }
            catch (Exception e)
            {
                Logger.ColoredConsoleWrite(ConsoleColor.Red, "Error evolvePokemon: " + e.Message);
                await evolvePokemon(pokemon);
            }
            return resp;
        }

        private static async Task<taskResponse> transferPokemon(PokemonData pokemon)
        {
            taskResponse resp = new taskResponse(false, string.Empty);
            try
            {
                var transferPokemonResponse = await client.Inventory.TransferPokemon(pokemon.Id);

                if (transferPokemonResponse.Result == ReleasePokemonResponse.Types.Result.Success)
                {
                    resp.Status = true;
                }
                else
                {
                    resp.Message = pokemon.PokemonId.ToString();
                }
            }
            catch (Exception e)
            {
                Logger.ColoredConsoleWrite(ConsoleColor.Red, "Error transferPokemon: " + e.Message);
                await transferPokemon(pokemon);
            }
            return resp;
        }

        private static async Task<taskResponse> PowerUp(PokemonData pokemon)
        {
            taskResponse resp = new taskResponse(false, string.Empty);
            try
            {
                var evolvePokemonResponse = await client.Inventory.UpgradePokemon(pokemon.Id);

                if (evolvePokemonResponse.Result == UpgradePokemonResponse.Types.Result.Success)
                {
                    resp.Status = true;
                }
                else
                {
                    resp.Message = pokemon.PokemonId.ToString();
                }

                await RandomHelper.RandomDelay(1000, 2000);
            }
            catch (Exception e)
            {
                Logger.ColoredConsoleWrite(ConsoleColor.Red, "Error Powering Up: " + e.Message);
                await PowerUp(pokemon);
            }
            return resp;
        }

        private static string IVsToNickname(PokemonData pokemon)
        {
            string croppedName = StringUtils.getPokemonNameByLanguage(ClientSettings, (PokemonId)pokemon.PokemonId) + " ";
            string nickname;
            //< nickname = string.Format("{0}{1}{2}{3}", pokemon.IndividualAttack.ToString("X"), pokemon.IndividualDefense.ToString("X"), pokemon.IndividualStamina.ToString("X"),(45 - pokemon.IndividualAttack- pokemon.IndividualDefense- pokemon.IndividualStamina));
            nickname = string.Format("{0}.{1}.{2}.{3}", PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0"), pokemon.IndividualAttack, pokemon.IndividualDefense, pokemon.IndividualStamina);
            int lenDiff = 12 - nickname.Length;
            if (croppedName.Length > lenDiff)
                croppedName = croppedName.Substring(0, lenDiff);
            return croppedName + nickname;
        }

        private static async Task<taskResponse> changePokemonNickname(PokemonData pokemon)
        {
            taskResponse resp = new taskResponse(false, string.Empty);
            try
            {
                var nicknamePokemonResponse1 = await client.Inventory.NicknamePokemon(pokemon.Id, pokemon.Nickname);

                if (nicknamePokemonResponse1.Result == NicknamePokemonResponse.Types.Result.Success)
                {
                    resp.Status = true;
                }
                else
                {
                    resp.Message = pokemon.PokemonId.ToString();
                }
            }
            catch (Exception e)
            {
                Logger.ColoredConsoleWrite(ConsoleColor.Red, "Error changePokemonNickname: " + e.Message);
                await changePokemonNickname(pokemon);
            }
            return resp;
        }

 		private static async Task<taskResponse> changeFavourites(PokemonData pokemon)
        {
            taskResponse resp = new taskResponse(false, string.Empty);
            try
            {
            	var response = await client.Inventory.SetFavoritePokemon( (long) pokemon.Id, (pokemon.Favorite == 1));

                if (response.Result == SetFavoritePokemonResponse.Types.Result.Success)
                {
                    resp.Status = true;
                }
                else
                {
                    resp.Message = pokemon.PokemonId.ToString();
                }
            }
            catch (Exception e)
            {
                Logger.ColoredConsoleWrite(ConsoleColor.Red, "Error ChangeFavourites: " + e.Message);
                await changeFavourites(pokemon);
            }
            return resp;
        }                

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (PokemonListView.SelectedItems.Count > 0 && PokemonListView.SelectedItems[0].Checked)
                contextMenuStrip1.Items[2].Visible = true;
        }

        private void contextMenuStrip1_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            contextMenuStrip1.Items[2].Visible = false;
        }

        private async void evolveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pokemon = (PokemonData)PokemonListView.SelectedItems[0].Tag;
            taskResponse resp = new taskResponse(false, string.Empty);

            if (MessageBox.Show(this, pokemon.PokemonId + " with " + pokemon.Cp + " CP thats " + Math.Round(PokemonInfo.CalculatePokemonPerfection(pokemon)) + "% perfect", "Are you sure you want to evolve?", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                resp = await evolvePokemon(pokemon);
            }
            else
            {
                return;
            }
            if (resp.Status)
            {
                PokemonListView.Items.Clear();
                Execute();
            }
            else
                MessageBox.Show(resp.Message + " evolving failed!", "Evolve Status", MessageBoxButtons.OK);
        }

        private async void powerUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pokemon = (PokemonData)PokemonListView.SelectedItems[0].Tag;
            taskResponse resp = new taskResponse(false, string.Empty);

            if (MessageBox.Show(this, pokemon.PokemonId + " with " + pokemon.Cp + " CP thats " + Math.Round(PokemonInfo.CalculatePokemonPerfection(pokemon)) + "% perfect", "Are you sure you want to power it up?", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                resp = await PowerUp(pokemon);
            }
            else
            {
                return;
            }
            if (resp.Status)
            {
                PokemonListView.Items.Clear();
                Execute();
            }
            else
                MessageBox.Show(resp.Message + " powering up failed!", "PowerUp Status", MessageBoxButtons.OK);
        }

        private async void IVsToNicknameToolStripMenuItemClick(object sender, EventArgs e)
        {
            var pokemon = (PokemonData)PokemonListView.SelectedItems[0].Tag;
            taskResponse resp = new taskResponse(false, string.Empty);

            string promptValue = Prompt.ShowDialog(IVsToNickname(pokemon), "Confirm Nickname");

            if (promptValue != "")
            {
                pokemon.Nickname = promptValue;
                resp = await changePokemonNickname(pokemon);
            }
            else
            {
                return;
            }
            if (resp.Status)
            {
                PokemonListView.SelectedItems[0].ToolTipText = new DateTime((long)pokemon.CreationTimeMs * 10000).AddYears(1969).ToString("dd/MM/yyyy HH:mm:ss");
                PokemonListView.SelectedItems[0].ToolTipText += "\nNickname: " + pokemon.Nickname;
            }
            else
                MessageBox.Show(resp.Message + " rename failed!", "Rename Status", MessageBoxButtons.OK);
        }
        
        private async void changeFavouritesToolStripMenuItemClick(object sender, EventArgs e)
        {
            var pokemon = (PokemonData)PokemonListView.SelectedItems[0].Tag;
            taskResponse resp = new taskResponse(false, string.Empty);

			string poname = StringUtils.getPokemonNameByLanguage(ClientSettings, (PokemonId)pokemon.PokemonId);
			if (MessageBox.Show(this, poname + " will be " +((pokemon.Favorite == 1)?"deleted from":"added to") + " your favourites." +"\nAre you sure you want?", "Confirmation Message", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
            	pokemon.Favorite =  (pokemon.Favorite == 1)?0:1 ;
                resp = await changeFavourites(pokemon);
            }
            else
            {
                return;
            }
            if (resp.Status)
            {
            	PokemonListView.SelectedItems[0].Text = string.Format((pokemon.Favorite == 1) ? "{0} ★" : "{0}", StringUtils.getPokemonNameByLanguage(ClientSettings, (PokemonId)pokemon.PokemonId));
            }
            else
                MessageBox.Show(resp.Message + " rename failed!", "Rename Status", MessageBoxButtons.OK);
        }        

        private void checkboxReload_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxreload.Checked)
            {
                int def = 0;
                int interval;
                if (int.TryParse(reloadsecondstextbox.Text, out interval))
                {
                    def = interval;
                }
                if (def < 30 || def > 3600)
                {
                    MessageBox.Show("Interval has to be between 30 and 3600 seconds!");
                    reloadsecondstextbox.Text = "60";
                    checkBoxreload.Checked = false;
                }
                else
                {
                    reloadtimer.Interval = def * 1000;
                    reloadtimer.Start();
                }
            }
            else
            {
                reloadtimer.Stop();
            }

        }

        private void reloadtimer_Tick(object sender, EventArgs e)
        {
            PokemonListView.Items.Clear();
            Execute();
        }

        private void reloadsecondstextbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private async void btnFullPowerUp_Click(object sender, EventArgs e)
        {
            //if (Globals.UseAnimationTimes)
            //{

            //}
            //else
            //{
            EnabledButton(false, "Powering up...");
            DialogResult result = MessageBox.Show("This process may take some time.", "FullPowerUp status", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                var selectedItems = PokemonListView.SelectedItems;
                int poweredup = 0;
                int total = selectedItems.Count;
                string failed = string.Empty;

                taskResponse resp = new taskResponse(false, string.Empty);
                int i = 0;
                int powerUps = 0;
                while (i == 0)
                {
                    var poweruplimit = 0;
                    int.TryParse(textBox1.Text, out poweruplimit);
                    foreach (ListViewItem selectedItem in selectedItems)
                    {
                        if (textBox1.Text != string.Empty)
                        {
                            if (poweruplimit > 0 && poweredup < poweruplimit)
                            {
                                resp = await PowerUp((PokemonData)selectedItem.Tag);
                                if (resp.Status)
                                {
                                    poweredup++;
                                }
                                else
                                    failed += resp.Message + " ";
                            }
                            else
                                failed += " Power Up Limit Reached ";
                        }
                        else
                        {
                            resp = await PowerUp((PokemonData)selectedItem.Tag);
                            if (resp.Status)
                            {
                                poweredup++;
                            }
                            else
                                failed += resp.Message + " ";
                        }
                    }
                    if (failed != string.Empty)
                    {
                        if (powerUps > 0)
                        {
                            MessageBox.Show("Pokemon succesfully powered " + powerUps + " times.", "FullPowerUp status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Pokemon not powered up. Not enough Stardust or Candy.", "FullPowerUp status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        i = 1;
                        EnabledButton(true);
                    }
                    else
                    {
                        powerUps++;
                        statusTexbox.Text = "Powering up..." + powerUps;
                        await RandomHelper.RandomDelay(1200, 1500);
                    }
                }
                if (poweredup > 0 && i == 1)
                {
                    PokemonListView.Items.Clear();
                    Execute();
                }
            }
            else
            {
                EnabledButton(true);
            }
            //}
        }

        private void btnShowMap_Click(object sender, EventArgs e)
        {
            if (stats == null)
            {
                MessageBox.Show("Stats not yet ready - please try again momentarily");
            }
            else
            {
                new LocationSelect(true, (int)profile.PlayerData.Team, stats.Level, stats.Experience).Show();
            }
        }

        private void lang_en_btn2_Click(object sender, EventArgs e)
        {
            lang_de_btn_2.Enabled = true;
            lang_spain_btn2.Enabled = true;
            lang_en_btn2.Enabled = false;
            lang_ptBR_btn2.Enabled = true;
            lang_tr_btn2.Enabled = true;
            languagestr2 = null;

            // Pokemon List GUI
            btnreload.Text = "Reload";
            btnEvolve.Text = "Evolve";
            checkBoxreload.Text = "Reload every";
            btnUpgrade.Text = "PowerUp";
            btnFullPowerUp.Text = "FULL-PowerUp";
            btnForceUnban.Text = "Force Unban";
            btnTransfer.Text = "Transfer";
        }

        private void lang_de_btn_2_Click(object sender, EventArgs e)
        {
            lang_en_btn2.Enabled = true;
            lang_spain_btn2.Enabled = true;
            lang_de_btn_2.Enabled = false;
            lang_ptBR_btn2.Enabled = true;
            lang_tr_btn2.Enabled = true;
            languagestr2 = "de";

            // Pokemon List GUI
            btnreload.Text = "Aktualisieren";
            btnEvolve.Text = "Entwickeln";
            checkBoxreload.Text = "Aktualisiere alle";
            btnUpgrade.Text = "PowerUp";
            btnFullPowerUp.Text = "FULL-PowerUp";
            btnForceUnban.Text = "Force Unban";
            btnTransfer.Text = "Versenden";
        }

        private void lang_spain_btn2_Click(object sender, EventArgs e)
        {
            lang_en_btn2.Enabled = true;
            lang_de_btn_2.Enabled = true;
            lang_spain_btn2.Enabled = false;
            lang_ptBR_btn2.Enabled = true;
            lang_tr_btn2.Enabled = true;
            languagestr2 = "spain";

            // Pokemon List GUI
            btnreload.Text = "Actualizar";
            btnEvolve.Text = "Evolucionar";
            checkBoxreload.Text = "Actualizar cada";
            btnUpgrade.Text = "Dar más poder";
            btnFullPowerUp.Text = "Dar más poder [TOTAL]";
            btnForceUnban.Text = "Force Unban";
            btnTransfer.Text = "Transferir";
        }

        private void lang_ptBR_btn2_Click(object sender, EventArgs e)
        {
            lang_en_btn2.Enabled = true;
            lang_de_btn_2.Enabled = true;
            lang_spain_btn2.Enabled = true;
            lang_ptBR_btn2.Enabled = false;
            lang_tr_btn2.Enabled = true;
            languagestr2 = "ptBR";

            // Pokemon List GUI
            btnreload.Text = "Recarregar";
            btnEvolve.Text = "Evoluir (selecionados)";
            checkBoxreload.Text = "Recarregar a cada";
            btnUpgrade.Text = "PowerUp (selecionados)";
            btnFullPowerUp.Text = "FULL-PowerUp (selecionados)";
            btnForceUnban.Text = "Force Unban";
            btnTransfer.Text = "Transferir (selecionados)";

        }

        private void lang_tr_btn2_Click(object sender, EventArgs e)
        {
            lang_de_btn_2.Enabled = true;
            lang_spain_btn2.Enabled = true;
            lang_en_btn2.Enabled = true;
            lang_ptBR_btn2.Enabled = true;
            lang_tr_btn2.Enabled = false;
            languagestr2 = "tr";

            // Pokemon List GUI
            btnreload.Text = "Yenile";
            btnEvolve.Text = "Geliştir";
            checkBoxreload.Text = "Yenile her";
            btnUpgrade.Text = "Güçlendir";
            btnFullPowerUp.Text = "TAM-Güçlendir";
            btnForceUnban.Text = "Banı Kaldırmaya Zorla";
            btnTransfer.Text = "Transfer";
        }

        private void btnForceUnban_Click(object sender, EventArgs e)
        {
            // **MTK4355 Repurposed force unban button since force-unban feature is no longer working**
            //Logic.Logic.failed_softban = 6;
            //btnForceUnban.Enabled = false;
            //freezedenshit.Start();
            if (btnForceUnban.Text.Equals("Pause Walking"))
            {
                Globals.pauseAtPokeStop = true;
                Logger.ColoredConsoleWrite(ConsoleColor.Magenta, "Pausing at next Pokestop. (will continue catching pokemon and farming pokestop when available)");
                if (Globals.RouteToRepeat.Count > 0)
                {
                    Logger.ColoredConsoleWrite(ConsoleColor.Yellow, "User Defined Route Cleared!");
                    Globals.RouteToRepeat.Clear();
                }

                btnForceUnban.Text = "Resume Walking";
                button2.Enabled = true;
                checkBox1.Enabled = true;
            }
            else
            {
                Globals.pauseAtPokeStop = false;
                Logger.ColoredConsoleWrite(ConsoleColor.Magenta, "Resume walking between Pokestops.");
                if (Globals.RouteToRepeat.Count > 0)
                {
                    foreach (var geocoord in Globals.RouteToRepeat)
                    {
                        Globals.NextDestinationOverride.AddLast(geocoord);
                    }
                    Logger.ColoredConsoleWrite(ConsoleColor.Yellow, "User Defined Route Captured! Beginning Route Momentarily.");
                }
                btnForceUnban.Text = "Pause Walking";
                button2.Enabled = false;
                checkBox1.Enabled = false;
            }

        }

        private void freezedenshit_Tick(object sender, EventArgs e)
        {
            btnForceUnban.Enabled = true;
            freezedenshit.Stop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Globals.UseLureGUIClick = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Globals.UseLuckyEggGUIClick = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Globals.UseIncenseGUIClick = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Globals.RepeatUserRoute = checkBox1.Checked;
        }


        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            Globals.useluckyegg = checkBox10.Checked;
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            Globals.UseAnimationTimes = checkBox9.Checked;
        }

        private void checkBox_FarmPokestops_CheckedChanged(object sender, EventArgs e)
        {
            Globals.farmPokestops = checkBox_FarmPokestops.Checked;
        }

        private void checkBox_CatchPokemon_CheckedChanged(object sender, EventArgs e)
        {
            Globals.CatchPokemon = checkBox_CatchPokemon.Checked;
        }

        private void checkBox_BreakAtLure_CheckedChanged(object sender, EventArgs e)
        {
            Globals.BreakAtLure = checkBox_BreakAtLure.Checked;
        }

        private void checkBox_UseLureAtBreak_CheckedChanged(object sender, EventArgs e)
        {
            Globals.UseLureAtBreak = checkBox_UseLureAtBreak.Checked;
        }

        private void checkBox_RandomlyReduceSpeed_CheckedChanged(object sender, EventArgs e)
        {
            Globals.RandomReduceSpeed = checkBox_RandomlyReduceSpeed.Checked;
        }

        private void checkBox_UseBreakIntervalAndLength_CheckedChanged(object sender, EventArgs e)
        {
            Globals.UseBreakFields = checkBox_UseBreakIntervalAndLength.Checked;
        }

        private void checkBox_UseGoogleMapsRouting_CheckedChanged(object sender, EventArgs e)
        {
            Globals.UseGoogleMapsAPI = checkBox_UseGoogleMapsRouting.Checked;
        }

        private void checkBox_WalkInArchimedeanSpiral_CheckedChanged(object sender, EventArgs e)
        {
            Globals.Espiral = checkBox_WalkInArchimedeanSpiral.Checked;
        }


        private void checkBox_RandomSleepAtCatching_CheckedChanged(object sender, EventArgs e)
        {
            Globals.sleepatpokemons = checkBox_RandomSleepAtCatching.Checked;
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            Globals.evolve = checkBox11.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Globals.pauseAtEvolve = checkBox2.Checked;
            Globals.pauseAtEvolve2 = checkBox2.Checked;
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            Globals.useincense = checkBox8.Checked;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            Globals.keepPokemonsThatCanEvolve = checkBox7.Checked;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            Globals.useLuckyEggIfNotRunning = checkBox6.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            Globals.userazzberry = checkBox3.Checked;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            Globals.autoIncubate = checkBox5.Checked;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            Globals.useBasicIncubators = checkBox4.Checked;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (!double.TryParse(textBox2.Text, out Globals.razzberry_chance)) Globals.razzberry_chance = 0;
        }

        private void text_GoogleMapsAPIKey_TextChanged(object sender, EventArgs e)
        {
            Globals.GoogleMapsAPIKey = text_GoogleMapsAPIKey.Text;
        }


        private void reloadbtn_Click(object sender, EventArgs e)
        {
            ItemsListView.Items.Clear();
            PokemonListView.Items.Clear();
            Execute();
        }
        
        private void text_Max(object sender, EventArgs e)
        {
            if (text_MaxPokeballs.Text != null &&
                text_MaxGreatBalls.Text != null &&
                text_MaxUltraBalls.Text != null &&
                text_MaxMasterBalls.Text != null &&
                text_MaxRevives.Text != null &&
                text_MaxTopRevives.Text != null &&
                text_MaxPotions.Text != null &&
                text_MaxSuperPotions.Text != null &&
                text_MaxHyperPotions.Text != null &&
                text_MaxTopPotions.Text != null &&
                text_MaxRazzBerrys.Text != null)
            {
                #region variablesetters
                int _pokeballs;
                int _greatballs;
                int _ultraballs;
                int _revives;
                int _potions;
                int _superpotions;
                int _hyperpotions;
                int _razzberrys;
                int _masterballs;
                int _toprevives;
                int _toppotions;
                #endregion

                #region variable parsers and sum total
                int itemSumme = 0;
                if (!int.TryParse(text_MaxPokeballs.Text, out _pokeballs)) _pokeballs = 20;
                itemSumme += _pokeballs;
                if (!int.TryParse(text_MaxUltraBalls.Text, out _greatballs)) _greatballs = 20;
                itemSumme += _greatballs;
                if (!int.TryParse(text_MaxUltraBalls.Text, out _ultraballs)) _ultraballs = 20;
                itemSumme += _ultraballs;
                if (!int.TryParse(text_MaxRevives.Text, out _revives)) _revives = 20;
                itemSumme += _revives;
                if (!int.TryParse(text_MaxPotions.Text, out _potions)) _potions = 20;
                itemSumme += _potions;
                if (!int.TryParse(text_MaxSuperPotions.Text, out _superpotions)) _superpotions = 20;
                itemSumme += _superpotions;
                if (!int.TryParse(text_MaxHyperPotions.Text, out _hyperpotions)) _hyperpotions = 20;
                itemSumme += _hyperpotions;
                if (!int.TryParse(text_MaxRazzBerrys.Text, out _razzberrys)) _razzberrys = 20;
                itemSumme += _razzberrys;
                if (!int.TryParse(text_MaxMasterBalls.Text, out _masterballs)) _masterballs = 200;
                itemSumme += _masterballs;
                if (!int.TryParse(text_MaxTopRevives.Text, out _toprevives)) _toprevives = 20;
                itemSumme += _toprevives;
                if (!int.TryParse(text_MaxTopPotions.Text, out _toppotions)) _toppotions = 20;
                itemSumme += _toppotions;
                #endregion

                #region rebuild recycle collection and sum total                
                Globals.pokeball = _pokeballs;
                Globals.greatball = _greatballs;
                Globals.ultraball = _ultraballs;
                Globals.revive = _revives;
                Globals.potion = _potions;
                Globals.superpotion = _superpotions;
                Globals.hyperpotion = _hyperpotions;
                Globals.berry = _razzberrys;
                Globals.masterball = _masterballs;
                Globals.toprevive = _toprevives;
                Globals.toppotion = _toppotions;
                text_TotalItemCount.Text = Convert.ToString(itemSumme);
                #endregion
            }
        }
        
        private void InitialzePokemonListView(){        			
        	PokemonListView.Columns.Clear();
        	ColumnHeader columnheader;
	        columnheader = new ColumnHeader();
	        columnheader.Name = "Name";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "CP";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "IV A-D-S";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "LVL";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "Evolvable?";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "Height";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "Weight";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "HP";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "Attack";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "SpecialAttack (DPS)";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "#";
	        columnheader.Text = columnheader.Name;	        
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "% CP";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "Type";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader);
	        columnheader = new ColumnHeader();
	        columnheader.Name = "Type 2";
	        columnheader.Text = columnheader.Name;
	        PokemonListView.Columns.Add(columnheader); 
	        
	        PokemonListView.Columns["#"].DisplayIndex = 0;
	        
	        PokemonListView.ColumnClick += new ColumnClickEventHandler(PokemonListView_ColumnClick);
            PokemonListView.ShowItemToolTips = true;
            PokemonListView.DoubleBuffered(true);
            PokemonListView.View = View.Details;

        }

		void BtnRealoadItemsClick(object sender, EventArgs e)
		{
            ItemsListView.Items.Clear();
            ExecuteItemsLoad();
		}                  
	}
    public static class ControlExtensions
    {
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }
    }
    // Compares two ListView items based on a selected column.
    public class ListViewComparer : System.Collections.IComparer
    {
        private int ColumnNumber;
        private SortOrder SortOrder;

        public ListViewComparer(int column_number, SortOrder sort_order)
        {
            ColumnNumber = column_number;
            SortOrder = sort_order;
        }

        // Compare two ListViewItems.
        public int Compare(object object_x, object object_y)
        {
            // Get the objects as ListViewItems.
            ListViewItem item_x = object_x as ListViewItem;
            ListViewItem item_y = object_y as ListViewItem;

            // Get the corresponding sub-item values.
            string string_x;
            if (item_x.SubItems.Count <= ColumnNumber)
            {
                string_x = "";
            }
            else
            {
                string_x = item_x.SubItems[ColumnNumber].Text;
            }

            string string_y;
            if (item_y.SubItems.Count <= ColumnNumber)
            {
                string_y = "";
            }
            else
            {
                string_y = item_y.SubItems[ColumnNumber].Text;
            }

            if (ColumnNumber == 2) //IV
            {
                string_x = string_x.Substring(0, string_x.IndexOf("%"));
                string_y = string_y.Substring(0, string_y.IndexOf("%"));

            }
            else if (ColumnNumber == 7) //HP
            {
                string_x = string_x.Substring(0, string_x.IndexOf("/"));
                string_y = string_y.Substring(0, string_y.IndexOf("/"));
            }

            // Compare them.
            int result;
            double double_x, double_y;
            if (double.TryParse(string_x, out double_x) &&
                double.TryParse(string_y, out double_y))
            {
                // Treat as a number.
                result = double_x.CompareTo(double_y);
            }
            else
            {
                DateTime date_x, date_y;
                if (DateTime.TryParse(string_x, out date_x) &&
                    DateTime.TryParse(string_y, out date_y))
                {
                    // Treat as a date.
                    result = date_x.CompareTo(date_y);
                }
                else
                {
                    // Treat as a string.
                    result = string_x.CompareTo(string_y);
                }
            }

            // Return the correct result depending on whether
            // we're sorting ascending or descending.
            if (SortOrder == SortOrder.Ascending)
            {
                return result;
            }
            else
            {
                return -result;
            }
        }
      
    
    }

}
