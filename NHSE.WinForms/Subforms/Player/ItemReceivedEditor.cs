﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NHSE.Core;

namespace NHSE.WinForms
{
    public partial class ItemReceivedEditor : Form
    {
        private readonly Player Player;

        public ItemReceivedEditor(Player player)
        {
            Player = player;
            InitializeComponent();
            this.TranslateInterface(GameInfo.CurrentLanguage);
            FillCheckBoxes();
            Initialize(GameInfo.Strings.ItemDataSource);
            CLB_Remake.SelectedIndex = 0;
            CLB_Items.SelectedIndex = 0x50; // clackercart
        }

        public void Initialize(List<ComboItem> items)
        {
            CB_Item.DisplayMember = nameof(ComboItem.Text);
            CB_Item.ValueMember = nameof(ComboItem.Value);
            CB_Item.DataSource = items;
        }

        private void FillCheckBoxes()
        {
            var items = GameInfo.Strings.itemlistdisplay;
            FillCollect(items);
            FillRemake(items);
        }

        private void FillCollect(IReadOnlyList<string> items)
        {
            var ofs = Player.Personal.Offsets.ItemCollectBit;
            var data = Player.Personal.Data;
            for (int i = 0; i < items.Count; i++)
            {
                var flag = FlagUtil.GetFlag(data, ofs, i);
                string value = items[i];
                string name = $"0x{i:X3} - {value}";
                CLB_Items.Items.Add(name, flag);
            }
        }

        private void FillRemake(IReadOnlyList<string> items)
        {
            var invert = ItemRemakeUtil.GetInvertedDictionary();
            var ofs = Player.Personal.Offsets.ItemRemakeCollectBit;
            var max = Player.Personal.Offsets.MaxRemakeBitFlag;
            var data = Player.Personal.Data;
            for (int i = 0; i < max; i++)
            {
                var remakeIndex = i >> 3;
                var variant = i & 7;

                ushort itemId = invert.TryGetValue((short)remakeIndex, out var id) ? id : (ushort)0;
                var itemName = remakeIndex == 0652 ? "photo" : items[itemId];

                var flag = FlagUtil.GetFlag(data, ofs, i);
                string name = $"{remakeIndex:0000} V{variant:0} - {itemName}";

                if (ItemRemakeInfoData.List.TryGetValue((short) remakeIndex, out var info))
                    name = $"{name} ({info.GetColorDescription(variant)})";

                CLB_Remake.Items.Add(name, flag);
            }
        }

        public void GiveAll(IReadOnlyList<ushort> indexes, bool value = true)
        {
            foreach (var item in indexes)
                CLB_Items.SetItemChecked(item, value);
            System.Media.SystemSounds.Asterisk.Play();
        }

        private void GiveEverything(IReadOnlyList<string> items, bool value = true)
        {
            if (!value)
            {
                for (ushort i = 0; i < CLB_Items.Items.Count; i++)
                    CLB_Items.SetItemChecked(i, false);
                return;
            }

            var skip = GameLists.NoCheckReceived;
            for (ushort i = 1; i < CLB_Items.Items.Count; i++)
            {
                if (string.IsNullOrEmpty(items[i]))
                    continue;
                if (skip.Contains(i))
                    continue;
                CLB_Items.SetItemChecked(i, true);
            }
            System.Media.SystemSounds.Asterisk.Play();
        }

        private static void ShowContextMenuBelow(ToolStripDropDown c, Control n) => c.Show(n.PointToScreen(new System.Drawing.Point(0, n.Height)));
        private void B_GiveAll_Click(object sender, EventArgs e) => ShowContextMenuBelow(CM_Buttons, (Control)sender);
        private void B_AllBugs_Click(object sender, EventArgs e) => GiveAll(GameLists.Bugs, ModifierKeys != Keys.Alt);
        private void B_AllFish_Click(object sender, EventArgs e) => GiveAll(GameLists.Fish, ModifierKeys != Keys.Alt);
        private void B_AllArt_Click(object sender, EventArgs e) => GiveAll(GameLists.Art, ModifierKeys != Keys.Alt);
        private void B_GiveEverything_Click(object sender, EventArgs e) => GiveEverything(GameInfo.Strings.itemlist, ModifierKeys != Keys.Alt);
        private void B_Cancel_Click(object sender, EventArgs e) => Close();

        private void B_Save_Click(object sender, EventArgs e)
        {
            var ofs = Player.Personal.Offsets.ItemCollectBit;
            var data = Player.Personal.Data;
            for (int i = 0; i < CLB_Items.Items.Count; i++)
                FlagUtil.SetFlag(data, ofs, i, CLB_Items.GetItemChecked(i));
            Close();
        }

        private void CB_Item_SelectedValueChanged(object sender, EventArgs e)
        {
            var index = WinFormsUtil.GetIndex(CB_Item);
            if ((uint)index >= CLB_Items.Items.Count)
                index = 0;
            CLB_Items.SelectedIndex = index;

            var remake = ItemRemakeUtil.GetRemakeIndex((ushort)index);
            if (remake > 0)
                CLB_Remake.SelectedIndex = remake * 8;
        }

        private void CLB_Items_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = CLB_Items.SelectedIndex;
            CB_Item.SelectedValue = index;
        }
    }
}
