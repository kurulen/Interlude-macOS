﻿using System;
using System.Drawing;
using Prelude.Net.Protocol.Packets;
using Interlude.Gameplay;
using Interlude.Interface.Widgets;
using Interlude.Graphics;

namespace Interlude.Interface.Dialogs
{
    public class ScoreInfoDialog : FadeDialog
    {
        ScoreInfoProvider Data;
        ScoreGraph Graph;

        public ScoreInfoDialog(ScoreInfoProvider data, Action<string> a) : base(a)
        {
            TL_DeprecateMe(100, ScreenUtils.ScreenHeight * 2 + 100, AnchorType.MIN, AnchorType.MIN).BR_DeprecateMe(100, -ScreenUtils.ScreenHeight * 2 + 100, AnchorType.MAX, AnchorType.MAX);
            Move(new Rect(100, 100, 100, 100));
            Data = data;
            AddChild(new TextBox(Data.FormattedAccuracy, AnchorType.MIN, 0, true, Color.White, Color.Black).BR_DeprecateMe(200, 100, AnchorType.MIN, AnchorType.MIN));
            AddChild((Graph = new ScoreGraph(data)).Reposition(20, 0, -200, 1, -20, 1, -20, 1));
            Game.Online.SendPacket(new PacketScore() { score = data.Score, chartHash = Game.Gameplay.CurrentChart.GetHash() });
        }

        public override void Draw(Rect bounds)
        {
            PreDraw(bounds);
            SpriteBatch.DrawRect(bounds, Color.FromArgb(127, 0, 0, 0));
            bounds = GetBounds(bounds);
            Game.Screens.DrawChartBackground(bounds, Game.Screens.DarkColor, 1f);
            ScreenUtils.DrawFrame(bounds, Color.White);
            DrawWidgets(bounds);
            PostDraw(bounds);
        }

        protected override void OnClosing()
        {
            base.OnClosing();
            Move(new Rect(100, ScreenUtils.ScreenHeight * 2 + 100, 100, -ScreenUtils.ScreenHeight * 2 + 100));
        }

        protected override void Close(string s)
        {
            base.Close(s);
            Graph.RequestRedraw(); //frees fbo
        }
    }
}
