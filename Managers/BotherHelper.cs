﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public class BotherHelper : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly AddonWatcher           _addons;


        private readonly List<ChoiceBotherSet> _bothersYesNo;

        private readonly List<BotherSet> _bothersTalk;
        private readonly HashSet<string> _autoSkipSpeakerList = new();
        public           bool            SkipNextTalk    { get; set; } = false;
        public           bool?           SelectNextYesNo { get; set; } = null;

        public BotherHelper(DalamudPluginInterface pluginInterface, AddonWatcher addons, PeonConfiguration configuration)
        {
            _pluginInterface = pluginInterface;
            _addons          = addons;
            _bothersYesNo    = configuration.BothersYesNo;
            _bothersTalk     = configuration.BothersTalk;

            _addons.OnSelectStringSetup += OnSelectStringSetup;
            _addons.OnSelectYesnoSetup  += OnYesNoSetup;
            _addons.OnTalkUpdate        += OnTalkUpdate;
        }

        public void Dispose()
        {
            _addons.OnSelectStringSetup -= OnSelectStringSetup;
            _addons.OnSelectYesnoSetup  -= OnYesNoSetup;
            _addons.OnTalkUpdate        -= OnTalkUpdate;
        }


        private void OnSelectStringSetup(IntPtr ptr, IntPtr _)
        {
            PtrSelectString selectStringPtr = ptr;
            for (var i = 0; i < selectStringPtr.Count; ++i)
            {
                var text = selectStringPtr.ItemText(i);
                if (text == "Tend Crop")
                {
                    PluginLog.Verbose("Clicked SelectString window due to match on {StringType}, {MatchType}: \"{Text}\".", 0, 0, text);
                    selectStringPtr.Select(i);
                }
                else if (text == "Harvest Crop")
                {
                    PluginLog.Verbose("Clicked SelectString window due to match on {StringType}, {MatchType}: \"{Text}\".", 0, 0, text);
                    selectStringPtr.Select(i);
                }
                else if (text == "Plant Seeds")
                {
                    PluginLog.Verbose("Clicked SelectString window due to match on {StringType}, {MatchType}: \"{Text}\".", 0, 0, text);
                    selectStringPtr.Select(i);
                }
            }
        }

        private void OnTalkUpdate(IntPtr ptr, IntPtr _)
        {
            PtrTalk talkPtr = ptr;
            var     speaker = talkPtr.Speaker();
            var     text    = talkPtr.Text();

            foreach (var set in _bothersTalk)
            {
                var click = SkipNextTalk
                 || set.StringType switch
                    {
                        BotherSet.Source.Main    => set.Matches(text),
                        BotherSet.Source.Speaker => set.Matches(speaker),
                        _                        => throw new InvalidEnumArgumentException(),
                    };

                void TmpDelegate(Framework _)
                {
                    if (talkPtr.IsVisible)
                        talkPtr.Click();
                    else
                        _pluginInterface.Framework.OnUpdateEvent -= TmpDelegate;
                }

                if (!click)
                    continue;

                SkipNextTalk                             =  false;
                _pluginInterface.Framework.OnUpdateEvent += TmpDelegate;
                PluginLog.Verbose("Clicked Talk window due to match on {StringType}, {MatchType}: \"{Text}\".", set.StringType, set.MatchType,
                    set.Text);
                break;
            }
        }

        private unsafe void OnYesNoSetup(IntPtr ptr, IntPtr updateData)
        {
            PtrSelectYesno selectPtr = ptr;

            if (SelectNextYesNo != null)
            {
                selectPtr.Click(SelectNextYesNo.Value);
                return;
            }

            SelectNextYesNo = null;
            var stringPtr = *(void**) ((byte*) updateData.ToPointer() + 0x8);
            var text      = Marshal.PtrToStringAnsi(new IntPtr(stringPtr));

            var yesText = selectPtr.YesText;
            var noText  = selectPtr.NoText;

            foreach (var (set, button) in _bothersYesNo)
            {
                bool? click = set.StringType switch
                {
                    BotherSet.Source.Main      => set.Matches(text) ? button : null,
                    BotherSet.Source.YesButton => set.Matches(yesText) ? button : null,
                    BotherSet.Source.NoButton  => set.Matches(yesText) ? button : null,
                    _                          => throw new InvalidEnumArgumentException(),
                };

                if (click == null)
                    continue;

                selectPtr.Click(click.Value);
                PluginLog.Verbose(
                    "Clicked \"{ButtonText}\" in Yesno window due to match on {StringType}, {MatchType}: \"{Text}\".",
                    click.Value ? yesText : noText, set.StringType, set.MatchType, set.Text);
            }
        }
    }
}