using Terraria.ModLoader;
using Terraria;
using System.Collections.Generic;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace AutoRegear
{
    internal class AutoRegear : Mod
    {
        internal UserInterface AutoRegearInterface;
        internal AutoRegearUI RegearUI;
        private GameTime LastUpdateUIGameTime;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                AutoRegearInterface = new UserInterface();
                RegearUI = new AutoRegearUI
                {
                    RegearEnabledButtonImage = ModContent.GetTexture("AutoRegear/AutoRegear_1"),
                    RegearDisabledButtonImage = ModContent.GetTexture("AutoRegear/AutoRegear_0"),
                    SetLoadoutButtonImage = ModContent.GetTexture("AutoRegear/SetLoadout_0"),
                    SetLoadoutButtonHoverImage = ModContent.GetTexture("AutoRegear/SetLoadout_1")
                };
                RegearUI.Activate();
                AutoRegearInterface.SetState(RegearUI);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            LastUpdateUIGameTime = gameTime;

            if (AutoRegearInterface?.CurrentState != null)
            {
                AutoRegearInterface.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int InventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (InventoryIndex != -1)
            {
                layers.Insert(InventoryIndex, new LegacyGameInterfaceLayer(
                    "AutoRegear: AutoRegearInterface",
                    delegate
                    {
                        if (LastUpdateUIGameTime != null && AutoRegearInterface?.CurrentState != null && Main.playerInventory && Main.LocalPlayer.chest == -1)
                        {
                            AutoRegearInterface.Draw(Main.spriteBatch, LastUpdateUIGameTime);
                        }
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
            }
        }

        internal class AutoRegearPlayer : ModPlayer
        {
            public List<Item> GearLoadout;
            public bool RegearEnabled;

            public override void Initialize()
            {
                GearLoadout = new List<Item>();
                for (int i = 0; i < 54; i++)
                {
                    GearLoadout.Add(new Item());
                }
                RegearEnabled = true;
            }

            public override TagCompound Save()
            {
                return new TagCompound
                {
                    ["GearLoadout"] = GearLoadout,
                    ["RegearEnabled"] = RegearEnabled,
                };
            }
            public override void Load(TagCompound tag)
            {
                GearLoadout = tag.Get<List<Item>>("GearLoadout");
                RegearEnabled = tag.GetBool("RegearEnabled");
                tag.Clear();
            }

            public void EnableDisableRegear()
            {
                Main.PlaySound(22);
                if (RegearEnabled)
                {
                    RegearEnabled = false;
                }
                else
                {
                    RegearEnabled = true;
                }
            }

            public void SetLoadout()
            {
                Main.PlaySound(7);
                GearLoadout = new List<Item>();
                for (int i = 0; i < 10; i++)
                {
                    GearLoadout.Add(player.inventory[i]);
                }
                for (int i = 0; i < 20; i++)
                {
                    GearLoadout.Add(player.armor[i]);
                }
                for (int i = 0; i < 5; i++)
                {
                    GearLoadout.Add(player.miscEquips[i]);
                }
                for (int i = 0; i < 4; i++)
                {
                    GearLoadout.Add(player.inventory[i + 54]);
                }
                for (int i = 0; i < 10; i++)
                {
                    GearLoadout.Add(player.dye[i]);
                }
                for (int i = 0; i < 5; i++)
                {
                    GearLoadout.Add(player.miscDyes[i]);
                }
            }

            public void ReturnSwappedItem(Item ItemToReturn)
            {
                bool InventoryFull = true;

                for (int i = 49; i >= 0; i--)
                {
                    if (ItemToReturn.IsTheSameAs(player.inventory[i]) && player.inventory[i].maxStack > 1 && player.inventory[i].stack < player.inventory[i].maxStack && ItemToReturn.prefix == player.inventory[i].prefix)
                    {
                        if (player.inventory[i].stack + ItemToReturn.stack <= player.inventory[i].maxStack)
                        {
                            player.inventory[i].stack += ItemToReturn.stack;
                            return;
                        }
                        else
                        {
                            int RequiredToFillStack = player.inventory[i].maxStack - player.inventory[i].stack;
                            player.inventory[i].stack += RequiredToFillStack;
                            ItemToReturn.stack -= RequiredToFillStack;
                            continue;
                        }
                    }
                }
                if (ItemToReturn.stack > 0)
                {
                    for (int i = 49; i >= 0; i--)
                    {
                        if (player.inventory[i].IsAir)
                        {
                            player.inventory[i] = ItemToReturn;
                            InventoryFull = false;
                            break;
                        }
                    }
                }
                if (InventoryFull && ItemToReturn.stack > 0)
                {
                    player.QuickSpawnClonedItem(ItemToReturn, ItemToReturn.stack);
                }
            }

            public void HandleStacks(Item StackedItem)
            {
                int ReturnStackSize = StackedItem.stack;

                for (int i = 0; i < StackedItem.stack; i++)
                {
                    Item SingleItem = new Item();
                    SingleItem.SetDefaults(StackedItem.netID);
                    SingleItem.stack = 1;
                    var RegearStackResult = RegearItem(SingleItem);
                    if (RegearStackResult.Item1)
                    {
                        SingleItem.stack = ReturnStackSize;
                        ReturnSwappedItem(SingleItem);
                        break;
                    }
                    else
                    {
                        if (RegearStackResult.Item2 != null)
                        {
                            while (RegearStackResult.Item2.netID != 0 && !RegearStackResult.Item1)
                            {
                                RegearStackResult = RegearItem(RegearStackResult.Item2);
                            }
                            ReturnSwappedItem(RegearStackResult.Item2);
                            ReturnStackSize -= 1;
                        }
                    }
                }
            }

            public (bool, Item) RegearItem(Item PickedUpItem)
            {
                var SwapItem = PickedUpItem;

                for (int i = 0; i < 54; i++)
                {
                    if (SwapItem.IsTheSameAs(GearLoadout[i]) && SwapItem.prefix == GearLoadout[i].prefix)
                    {
                        if (i < 10)
                        {
                            SwapItem = player.inventory[i];
                            if (SwapItem.IsTheSameAs(PickedUpItem) && SwapItem.prefix == PickedUpItem.prefix)
                            {
                                SwapItem = PickedUpItem;
                                continue;
                            }
                            player.inventory[i] = PickedUpItem;
                        }
                        else if (i < 30)
                        {
                            int ArmorSlot = i - 10;
                            SwapItem = player.armor[ArmorSlot];
                            if (SwapItem.IsTheSameAs(PickedUpItem) && SwapItem.prefix == PickedUpItem.prefix)
                            {
                                SwapItem = PickedUpItem;
                                continue;
                            }
                            player.armor[ArmorSlot] = PickedUpItem;
                        }
                        else if (i < 35)
                        {
                            int MiscSlot = i - 30;
                            SwapItem = player.miscEquips[MiscSlot];
                            if (SwapItem.IsTheSameAs(PickedUpItem) && SwapItem.prefix == PickedUpItem.prefix)
                            {
                                SwapItem = PickedUpItem;
                                continue;
                            }
                            player.miscEquips[MiscSlot] = PickedUpItem;
                        }
                        else if (i < 39)
                        {
                            int AmmoSlot = i + 19;
                            SwapItem = player.inventory[AmmoSlot];
                            if (SwapItem.IsTheSameAs(PickedUpItem) && SwapItem.prefix == PickedUpItem.prefix)
                            {
                                SwapItem = PickedUpItem;
                                continue;
                            }
                            player.inventory[AmmoSlot] = PickedUpItem;
                        }
                        else if (i < 49)
                        {
                            if (PickedUpItem.stack > 1)
                            {
                                HandleStacks(PickedUpItem);
                                SwapItem = null;
                            }
                            else
                            {
                                int DyeSlot = i - 39;
                                SwapItem = player.dye[DyeSlot];
                                if (SwapItem.IsTheSameAs(PickedUpItem) && SwapItem.prefix == PickedUpItem.prefix)
                                {
                                    SwapItem = PickedUpItem;
                                    continue;
                                }
                                player.dye[DyeSlot] = PickedUpItem;
                            }
                        }
                        else if (i < 54)
                        {
                            if (PickedUpItem.stack > 1)
                            {
                                HandleStacks(PickedUpItem);
                                SwapItem = null;
                            }
                            else
                            {
                                int MiscDyeSlot = i - 49;
                                SwapItem = player.miscDyes[MiscDyeSlot];
                                if (SwapItem.IsTheSameAs(PickedUpItem) && SwapItem.prefix == PickedUpItem.prefix)
                                {
                                    SwapItem = PickedUpItem;
                                    continue;
                                }
                                player.miscDyes[MiscDyeSlot] = PickedUpItem;
                            }
                        }
                        else
                        {
                            return (true, SwapItem);
                        }
                        return (false, SwapItem);
                    }
                }
                return (true, PickedUpItem);
            }
        }

        class AutoRegearGlobalItem : GlobalItem
        {
            public override bool OnPickup(Item item, Player player)
            {
                AutoRegearPlayer PlayerToRegear = player.GetModPlayer<AutoRegearPlayer>();

                if (PlayerToRegear.RegearEnabled)
                {
                    var RegearResult = PlayerToRegear.RegearItem(item);

                    if (!RegearResult.Item1)
                    {
                        //Display pickup text and play pic	kup sound for AutoRegeared item
                        ItemText.NewText(item, item.stack);
                        Main.PlaySound(7);
                        //Repeat process for swapped out item
                        if (RegearResult.Item2 != null)
                        {
                            while (RegearResult.Item2.netID != 0 && !RegearResult.Item1)
                            {
                                RegearResult = PlayerToRegear.RegearItem(RegearResult.Item2);
                            }
                            PlayerToRegear.ReturnSwappedItem(RegearResult.Item2);
                        }
                        return false;
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }
        }

        internal class AutoRegearButton : UIImage
        {
            public AutoRegearButton(Texture2D texture) : base(texture)
            {
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                if (ContainsPoint(Main.MouseScreen))
                {
                    Main.LocalPlayer.mouseInterface = true;
                }
            }
        }

        internal class AutoRegearUI : UIState
        {
            internal AutoRegearButton AutoRegearToggleEnable;
            internal AutoRegearButton AutoRegearSetLoadout;
            internal Texture2D RegearEnabledButtonImage;
            internal Texture2D RegearDisabledButtonImage;
            internal Texture2D SetLoadoutButtonImage;
            internal Texture2D SetLoadoutButtonHoverImage;


            public override void OnInitialize()
            {
                AutoRegearToggleEnable = new AutoRegearButton(RegearDisabledButtonImage);
                AutoRegearToggleEnable.Left.Set(410, 0f);
                AutoRegearToggleEnable.Top.Set(261, 0f);
                AutoRegearToggleEnable.OnClick += OnRegearEnableClick;
                AutoRegearToggleEnable.OnMouseOver += OnButtonHover;
                AutoRegearToggleEnable.OnMouseOut += OnButtonHover;
                Append(AutoRegearToggleEnable);
                AutoRegearSetLoadout = new AutoRegearButton(SetLoadoutButtonImage);
                AutoRegearSetLoadout.Left.Set(370, 0f);
                AutoRegearSetLoadout.Top.Set(261, 0f);
                AutoRegearSetLoadout.OnClick += OnSetLoadoutClick;
                AutoRegearSetLoadout.OnMouseOver += OnButtonHover;
                AutoRegearSetLoadout.OnMouseOut += OnButtonHover;
                Append(AutoRegearSetLoadout);
            }

            private void OnRegearEnableClick(UIMouseEvent evt, UIElement listeningElement)
            {
                Main.LocalPlayer.GetModPlayer<AutoRegearPlayer>().EnableDisableRegear();
            }

            private void OnSetLoadoutClick(UIMouseEvent evt, UIElement listeningElement)
            {
                Main.LocalPlayer.GetModPlayer<AutoRegearPlayer>().SetLoadout();
            }

            private void OnButtonHover(UIMouseEvent evt, UIElement listeningElement)
            {
                Main.PlaySound(12);
                if (ContainsPoint(Main.MouseScreen))
                {
                    Main.LocalPlayer.mouseInterface = true;
                }
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (AutoRegearToggleEnable.IsMouseHovering)
                {
                    AutoRegearToggleEnable.SetImage(RegearEnabledButtonImage);
                    if (Main.LocalPlayer.GetModPlayer<AutoRegearPlayer>().RegearEnabled)
                    {
                        Main.hoverItemName = "Disable AutoRegear";
                    }
                    else
                    {
                        Main.hoverItemName = "Enable AutoRegear";
                    }
                }
                else
                {
                    if (Main.LocalPlayer.GetModPlayer<AutoRegearPlayer>().RegearEnabled)
                    {
                        AutoRegearToggleEnable.SetImage(RegearEnabledButtonImage);
                    }
                    else
                    {
                        AutoRegearToggleEnable.SetImage(RegearDisabledButtonImage);
                    }
                if (AutoRegearSetLoadout.IsMouseHovering)
                    {
                        AutoRegearSetLoadout.SetImage(SetLoadoutButtonHoverImage);
                        Main.hoverItemName = "Set Gear Loadout";
                    }
                else
                    {
                        AutoRegearSetLoadout.SetImage(SetLoadoutButtonImage);
                    }
                }
            }

        }
    }
}