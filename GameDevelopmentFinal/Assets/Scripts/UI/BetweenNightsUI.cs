using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tylerland.Core;
using Tylerland.Animatronics;
using Tylerland.Office;

namespace Tylerland.UI
{
    /// <summary>
    /// Between-Nights Hub UGUI screen shown after 6:00 AM (NightEnd / Salary / Choice / Shop / Minigame),
    /// as well as clean Victory & Game Over (Dead) overlays.
    /// Provides the 3 requested Between-Nights options:
    /// 1. Soul Coin Shop (permanent meta-upgrades & weapon unlocks using Soul Coins)
    /// 2. Shop (run-only door repairs & upgrades using Shift Cash $)
    /// 3. Minigame (playable 20-second Security Signal Calibration arcade minigame awarding Cash & Soul Coins)
    /// </summary>
    public class BetweenNightsUI : MonoBehaviour
    {
        private enum HubSubView
        {
            Hub,
            SoulCoinShop,
            RunShop,
            Minigame,
            Victory,
            GameOver
        }

        private Canvas _canvas;
        private GameObject _rootPanel;
        private Text _headerTitle;
        private Text _headerSub;
        private Text _currencyText;
        private Text _feedbackText;
        private Transform _contentContainer;
        private Button _nextNightBtn;
        private Text _nextNightBtnText;
        private Button _backToHubBtn;

        private HubSubView _subView = HubSubView.Hub;
        private SalaryBreakdown? _lastSalary;
        private int _lastTransitionNight = -1;
        private bool _minigamePlayedThisTransition = false;

        // Minigame runtime state (Circuit Breaker / Signal Calibration)
        private bool _mgRunning = false;
        private float _mgTimeLeft = 20f;
        private int _mgScore = 0;
        private int _mgMisses = 0;
        private float _mgSpawnTimer = 0f;
        private int _mgActiveIndex = -1;
        private bool _mgIsGlitchTrap = false;
        private readonly Button[] _mgGridButtons = new Button[9];
        private readonly Image[] _mgGridImages = new Image[9];
        private readonly Text[] _mgGridLabels = new Text[9];
        private Text _mgStatusText;

        private void Awake()
        {
            BuildCanvas();
            SetVisible(false);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnSalaryCalculated += HandleSalaryCalculated;
            GameEvents.OnMoneyChanged += HandleCurrencyChanged;
            GameEvents.OnSoulCoinsChanged += HandleCurrencyChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnSalaryCalculated -= HandleSalaryCalculated;
            GameEvents.OnMoneyChanged -= HandleCurrencyChanged;
            GameEvents.OnSoulCoinsChanged -= HandleCurrencyChanged;
        }

        private void Start()
        {
            if (GameFlowManager.HasInstance)
            {
                HandleGameStateChanged(GameFlowManager.Instance.CurrentState, GameFlowManager.Instance.CurrentState);
            }
        }

        private void HandleSalaryCalculated(SalaryBreakdown sb)
        {
            _lastSalary = sb;
        }

        private void HandleCurrencyChanged(int newTotal, int delta)
        {
            if (_rootPanel != null && _rootPanel.activeSelf)
            {
                UpdateCurrencyHeader();
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.NightEnd || newState == GameState.Salary || newState == GameState.Choice)
            {
                int night = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;
                int totalNights = RunManager.HasInstance ? RunManager.Instance.TotalNights : 5;

                if (_lastTransitionNight != night)
                {
                    _lastTransitionNight = night;
                    _minigamePlayedThisTransition = false;
                }

                SetVisible(true);
                if (night >= totalNights)
                {
                    OpenSubView(HubSubView.Victory);
                }
                else
                {
                    OpenSubView(HubSubView.Hub);
                }
            }
            else if (newState == GameState.Victory)
            {
                SetVisible(true);
                OpenSubView(HubSubView.Victory);
            }
            else if (newState == GameState.Dead)
            {
                SetVisible(true);
                OpenSubView(HubSubView.GameOver);
            }
            else if (newState == GameState.Shop)
            {
                SetVisible(true);
                OpenSubView(HubSubView.RunShop);
            }
            else if (newState == GameState.Minigame)
            {
                SetVisible(true);
                OpenSubView(HubSubView.Minigame);
            }
            else
            {
                _mgRunning = false;
                SetVisible(false);
            }
        }

        private void Update()
        {
            if (!_mgRunning || _subView != HubSubView.Minigame) return;

            _mgTimeLeft -= Time.unscaledDeltaTime;
            _mgSpawnTimer -= Time.unscaledDeltaTime;

            if (_mgTimeLeft <= 0f)
            {
                FinishMinigame();
                return;
            }

            if (_mgSpawnTimer <= 0f)
            {
                SpawnNextMinigameNode();
            }

            if (_mgStatusText != null)
            {
                _mgStatusText.text = $"TIME: <b>{_mgTimeLeft:F1}s</b>   |   SIGNALS CALIBRATED: <color=#4ade80><b>{_mgScore}</b></color>   |   GLITCH HITS: <color=#f87171><b>{_mgMisses}</b></color>";
            }
        }

        private void SetVisible(bool visible)
        {
            if (_rootPanel != null) _rootPanel.SetActive(visible);
            if (visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void BuildCanvas()
        {
            _canvas = TylerlandUIKit.CreateOverlayCanvas("BetweenNights_Canvas", 400, transform);
            var backdrop = TylerlandUIKit.CreatePanel(_canvas.transform, "Backdrop", TylerlandUIKit.BgBackdrop, true);
            _rootPanel = backdrop.gameObject;

            // Main Center Container
            var frame = TylerlandUIKit.CreatePanel(backdrop, "HubFrame", TylerlandUIKit.PanelDark, false);
            frame.anchorMin = new Vector2(0.08f, 0.06f);
            frame.anchorMax = new Vector2(0.92f, 0.94f);
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;
            TylerlandUIKit.AddBorder(frame.gameObject, TylerlandUIKit.AccentCrimson, 2.5f);

            var vlg = frame.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(36, 36, 28, 28);
            vlg.spacing = 16f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Header Row
            var headerRow = new GameObject("HeaderRow", typeof(RectTransform));
            headerRow.transform.SetParent(frame, false);
            var headerHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerHlg.childForceExpandWidth = true;
            headerHlg.childForceExpandHeight = false;
            var headerLe = headerRow.AddComponent<LayoutElement>();
            headerLe.preferredHeight = 82f;

            var titleCol = new GameObject("TitleCol", typeof(RectTransform));
            titleCol.transform.SetParent(headerRow.transform, false);
            var titleVlg = titleCol.AddComponent<VerticalLayoutGroup>();
            titleVlg.spacing = 4f;
            _headerTitle = TylerlandUIKit.CreateText(titleCol.transform, "★ 6:00 AM — BETWEEN NIGHTS", 38, TylerlandUIKit.AccentGold, TextAnchor.MiddleLeft, FontStyle.Bold);
            _headerSub = TylerlandUIKit.CreateText(titleCol.transform, "Prepare for your next shift at Tylerland", 18, TylerlandUIKit.TextMuted, TextAnchor.MiddleLeft, FontStyle.Italic);

            _currencyText = TylerlandUIKit.CreateText(headerRow.transform, "", 22, TylerlandUIKit.TextPrimary, TextAnchor.MiddleRight, FontStyle.Bold);

            // Divider
            var div = TylerlandUIKit.CreatePanel(frame, "Divider", TylerlandUIKit.BorderSubtle, false);
            var divLe = div.gameObject.AddComponent<LayoutElement>();
            divLe.preferredHeight = 2f;

            // Feedback Banner
            _feedbackText = TylerlandUIKit.CreateText(frame, "", 18, new Color(0.4f, 0.95f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var fbLe = _feedbackText.gameObject.AddComponent<LayoutElement>();
            fbLe.preferredHeight = 28f;

            // Dynamic Content Area
            var contentObj = new GameObject("ContentContainer", typeof(RectTransform));
            contentObj.transform.SetParent(frame, false);
            var contentVlg = contentObj.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 14f;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            var contentLe = contentObj.AddComponent<LayoutElement>();
            contentLe.flexibleHeight = 1f;
            _contentContainer = contentObj.transform;

            // Footer Navigation Row
            var footerRow = new GameObject("FooterRow", typeof(RectTransform));
            footerRow.transform.SetParent(frame, false);
            var footerHlg = footerRow.AddComponent<HorizontalLayoutGroup>();
            footerHlg.spacing = 18f;
            footerHlg.childForceExpandWidth = true;
            footerHlg.childForceExpandHeight = false;
            var footerLe = footerRow.AddComponent<LayoutElement>();
            footerLe.preferredHeight = 62f;

            _backToHubBtn = TylerlandUIKit.CreateButton(footerRow.transform, "← BACK TO BETWEEN-NIGHTS HUB", TylerlandUIKit.BtnNeutral, () =>
            {
                _mgRunning = false;
                OpenSubView(HubSubView.Hub);
            }, 58f, 20);

            _nextNightBtn = TylerlandUIKit.CreateButton(footerRow.transform, "▶ START NEXT NIGHT", TylerlandUIKit.AccentEmerald, OnStartNextNightClicked, 58f, 24);
            _nextNightBtnText = _nextNightBtn.GetComponentInChildren<Text>();
        }

        private void ClearContent()
        {
            if (_contentContainer == null) return;
            for (int i = _contentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_contentContainer.GetChild(i).gameObject);
            }
        }

        private void UpdateCurrencyHeader()
        {
            int money = RunManager.HasInstance ? RunManager.Instance.Money : 0;
            int souls = MetaProgressionManager.HasInstance ? MetaProgressionManager.Instance.SoulCoins : 0;
            int night = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;
            int total = RunManager.HasInstance ? RunManager.Instance.TotalNights : 5;
            _currencyText.text = $"SHIFT CASH: <color=#4ade80>${money}</color>   |   SOUL COINS: <color=#c084fc>✦ {souls}</color>\n<size=16><color=#94a3b8>5-Night Cycle Progress: Night {night} / {total}</color></size>";
        }

        private void ShowFeedback(string msg, bool isError = false)
        {
            if (_feedbackText == null) return;
            _feedbackText.color = isError ? new Color(0.98f, 0.42f, 0.42f) : new Color(0.4f, 0.95f, 0.55f);
            _feedbackText.text = msg;
        }

        private void OpenSubView(HubSubView view)
        {
            _subView = view;
            _mgRunning = false;
            ClearContent();
            UpdateCurrencyHeader();
            ShowFeedback("");

            int currentNight = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;
            int totalNights = RunManager.HasInstance ? RunManager.Instance.TotalNights : 5;

            _backToHubBtn.gameObject.SetActive(view == HubSubView.SoulCoinShop || view == HubSubView.RunShop || view == HubSubView.Minigame);
            _nextNightBtn.gameObject.SetActive(view == HubSubView.Hub || view == HubSubView.SoulCoinShop || view == HubSubView.RunShop);

            if (view == HubSubView.Hub)
            {
                _headerTitle.text = $"★ 6:00 AM — NIGHT {currentNight} COMPLETE";
                _headerSub.text = $"Choose how to prepare before Night {currentNight + 1} begins";
                _nextNightBtnText.text = $"▶ START NIGHT {currentNight + 1}";
                BuildHubOverview(currentNight);
            }
            else if (view == HubSubView.SoulCoinShop)
            {
                _headerTitle.text = "✦ SOUL COIN SHOP";
                _headerSub.text = "Permanent upgrades & counter-weapons — kept forever across all 5-night runs";
                _nextNightBtnText.text = currentNight >= totalNights ? "🏠 RETURN TO MAIN MENU" : $"▶ START NIGHT {currentNight + 1}";
                BuildSoulCoinShop();
            }
            else if (view == HubSubView.RunShop)
            {
                _headerTitle.text = "🛒 SECURITY SHOP (CURRENT RUN)";
                _headerSub.text = "Spend your Shift Cash ($) on door repairs & hardware upgrades for this 5-night cycle";
                _nextNightBtnText.text = $"▶ START NIGHT {currentNight + 1}";
                BuildRunShop();
            }
            else if (view == HubSubView.Minigame)
            {
                _headerTitle.text = "🎮 BETWEEN-NIGHTS MINIGAME: SIGNAL CALIBRATION";
                _headerSub.text = "Click AMBER/GREEN nodes quickly (+1 pt). Avoid RED Nightmare glitches (-2 pts). Score 8+ for +1 Soul Coin!";
                BuildMinigameView();
            }
            else if (view == HubSubView.Victory)
            {
                _headerTitle.text = "🏆 SHIFT CYCLE COMPLETE — YOU SURVIVED TYLERLAND!";
                _headerSub.text = RunManager.HasInstance && RunManager.Instance.State.AllFourNightmaresActive
                    ? "TRUE ENDING ACHIEVED — All 4 Permanent Nightmares Conquered!"
                    : "All 5 Nights Survived! Trigger all 4 Nightmares in one run to unlock Night 6 True Boss.";
                BuildVictoryView();
            }
            else if (view == HubSubView.GameOver)
            {
                _headerTitle.text = "☠ SHIFT TERMINATED — YOU DIED";
                _headerSub.text = "Run upgrades & Nightmare states reset. Your Soul Coins & permanent unlocks are preserved.";
                BuildGameOverView();
            }
        }

        private void BuildHubOverview(int completedNight)
        {
            // 1. Paycheck & Nightmare Threat Intel Strip
            var infoRow = new GameObject("InfoStrip", typeof(RectTransform));
            infoRow.transform.SetParent(_contentContainer, false);
            var infoHlg = infoRow.AddComponent<HorizontalLayoutGroup>();
            infoHlg.spacing = 18f;
            infoHlg.childForceExpandWidth = true;
            var infoLe = infoRow.AddComponent<LayoutElement>();
            infoLe.preferredHeight = 155f;

            // Left Card: Paycheck Breakdown
            var payCard = TylerlandUIKit.CreatePanel(infoRow.transform, "PaycheckCard", TylerlandUIKit.CardBg, false);
            TylerlandUIKit.AddBorder(payCard.gameObject, TylerlandUIKit.BorderSubtle, 1.5f);
            var payVlg = payCard.gameObject.AddComponent<VerticalLayoutGroup>();
            payVlg.padding = new RectOffset(20, 20, 14, 14);
            payVlg.spacing = 6f;

            var sb = _lastSalary ?? new SalaryBreakdown(completedNight, 100 + (completedNight - 1) * 50, 0, 0, 100 + (completedNight - 1) * 50);
            TylerlandUIKit.CreateText(payCard, $"NIGHT {completedNight} PAYCHECK SUMMARY", 20, TylerlandUIKit.AccentGold, TextAnchor.MiddleLeft, FontStyle.Bold);
            TylerlandUIKit.CreateText(payCard,
                $"Base Pay: <color=#4ade80>+${sb.baseSalary}</color>   |   Clean Bonus: <color=#4ade80>+${sb.cleanNightBonus}</color>   |   Repair Deductions: <color=#f87171>-${sb.repairDeductions}</color>\n" +
                $"<b>Net Earned Tonight: <color=#38bdf8>+${sb.totalSalary}</color></b>",
                17, TylerlandUIKit.TextPrimary, TextAnchor.UpperLeft);

            // Right Card: Permanent Nightmare Escalation Forecast
            var threatCard = TylerlandUIKit.CreatePanel(infoRow.transform, "NightmareCard", TylerlandUIKit.CardBg, false);
            TylerlandUIKit.AddBorder(threatCard.gameObject, TylerlandUIKit.AccentCrimson, 1.5f);
            var threatVlg = threatCard.gameObject.AddComponent<VerticalLayoutGroup>();
            threatVlg.padding = new RectOffset(20, 20, 14, 14);
            threatVlg.spacing = 6f;

            TylerlandUIKit.CreateText(threatCard, $"NIGHT {completedNight + 1} NIGHTMARE INTEL", 20, new Color(1f, 0.4f, 0.45f), TextAnchor.MiddleLeft, FontStyle.Bold);
            TylerlandUIKit.CreateText(threatCard, BuildNightmareForecastString(completedNight + 1), 16, TylerlandUIKit.TextPrimary, TextAnchor.UpperLeft);

            // 2. Three Big Between-Nights Option Cards: SOUL COIN SHOP | SHOP | MINIGAME
            var optionsRow = new GameObject("ThreeOptionsRow", typeof(RectTransform));
            optionsRow.transform.SetParent(_contentContainer, false);
            var optHlg = optionsRow.AddComponent<HorizontalLayoutGroup>();
            optHlg.spacing = 24f;
            optHlg.childForceExpandWidth = true;
            optHlg.childForceExpandHeight = true;
            var optLe = optionsRow.AddComponent<LayoutElement>();
            optLe.flexibleHeight = 1f;
            optLe.minHeight = 320f;

            BuildOptionCard(
                optionsRow.transform,
                "✦ SOUL COIN SHOP",
                "PERMANENT UPGRADES",
                "Spend Soul Coins (✦) to permanently unlock Counter-Weapons and passive perks kept across ALL runs.",
                TylerlandUIKit.AccentPurple,
                "OPEN SOUL COIN SHOP",
                () => OpenSubView(HubSubView.SoulCoinShop));

            BuildOptionCard(
                optionsRow.transform,
                "🛒 SECURITY SHOP",
                "THIS 5-NIGHT RUN",
                "Spend Shift Cash ($) to repair damaged doors and install Battery, Plating, Motor, and Hydraulic upgrades.",
                TylerlandUIKit.AccentBlue,
                "OPEN SHOP",
                () => OpenSubView(HubSubView.RunShop));

            string mgDesc = _minigamePlayedThisTransition
                ? "Signal Calibration already completed for tonight's break! Available again after the next shift."
                : "Play the 20-second Signal Calibration security minigame to earn bonus Shift Cash ($) and Soul Coins (✦)!";
            var mgBtn = BuildOptionCard(
                optionsRow.transform,
                "🎮 MINIGAME",
                _minigamePlayedThisTransition ? "COMPLETED TONIGHT" : "BONUS REWARDS",
                mgDesc,
                TylerlandUIKit.AccentGold,
                _minigamePlayedThisTransition ? "ALREADY PLAYED" : "PLAY MINIGAME",
                () =>
                {
                    if (!_minigamePlayedThisTransition) OpenSubView(HubSubView.Minigame);
                });
            if (_minigamePlayedThisTransition && mgBtn != null) mgBtn.interactable = false;
        }

        private Button BuildOptionCard(
            Transform parent,
            string title,
            string badge,
            string desc,
            Color accentColor,
            string buttonText,
            UnityEngine.Events.UnityAction onClick)
        {
            var card = TylerlandUIKit.CreatePanel(parent, "Card_" + title, TylerlandUIKit.CardBg, false);
            TylerlandUIKit.AddBorder(card.gameObject, accentColor, 2f);

            var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.spacing = 12f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Top Accent Strip
            var strip = TylerlandUIKit.CreatePanel(card, "AccentBar", accentColor, false);
            var stripLe = strip.gameObject.AddComponent<LayoutElement>();
            stripLe.preferredHeight = 6f;

            var badgeTxt = TylerlandUIKit.CreateText(card, badge, 15, accentColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            badgeTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var titleTxt = TylerlandUIKit.CreateText(card, title, 28, TylerlandUIKit.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            titleTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            var descTxt = TylerlandUIKit.CreateText(card, desc, 18, TylerlandUIKit.TextMuted, TextAnchor.UpperCenter, FontStyle.Normal);
            var descLe = descTxt.gameObject.AddComponent<LayoutElement>();
            descLe.flexibleHeight = 1f;

            return TylerlandUIKit.CreateButton(card, buttonText, accentColor, onClick, 56f, 20);
        }

        private string BuildNightmareForecastString(int nextNight)
        {
            if (!RunManager.HasInstance) return "No animatronic intel available.";
            var st = RunManager.Instance.State;
            string[,] anims =
            {
                { "sandy", "Sandy (Rear)" },
                { "orville", "Orville (Right)" },
                { "erle", "Erle (Left)" },
                { "tyler", "Tyler (Center)" }
            };

            var parts = new List<string>();
            int nightmareCount = 0;
            for (int i = 0; i < 4; i++)
            {
                string id = anims[i, 0];
                string label = anims[i, 1];
                if (st.IsNightmareState(id))
                {
                    nightmareCount++;
                    int shotN = st.GetShotNight(id);
                    int bonus = st.GetNightmareNightBonus(id, nextNight);
                    parts.Add($"<color=#ff4d6d><b>{label}: PERMANENT NIGHTMARE</b> (Shot N{shotN} → +{bonus} AI Surge)</color>");
                }
                else
                {
                    parts.Add($"<color=#94a3b8>{label}: Normal</color>");
                }
            }

            if (nightmareCount == 0)
            {
                return "All animatronics are currently in Normal state.\n<color=#94a3b8>Remember: each gun has 1 use per 5-night run and permanently enrages its target.</color>";
            }
            return string.Join("\n", parts);
        }

        private void BuildSoulCoinShop()
        {
            var grid = CreateShopGrid(_contentContainer);

            AddSoulShopItem(grid, "SOUL_EXTRA_ENERGY", "Soul Capacitor", "+15% permanent max door energy across all runs.", 3);
            AddSoulShopItem(grid, "SOUL_EXTRA_DURABILITY", "Spectral Plating", "+20% permanent max door durability across all runs.", 3);
            AddSoulShopItem(grid, "SOUL_START_CASH", "Shift Stipend", "Start every new 5-night cycle with +$100 cash.", 2);
            AddSoulShopItem(grid, "SOUL_LIGHT_EFFICIENCY", "Night Vision Lens", "Halves hallway blindspot light energy drain permanently.", 4);
            AddSoulShopItem(grid, "Electric Gun", "Weapon: Electric Gun", "1 use/run vs Erle (Left). Triggers permanent Nightmare Erle.", 2, isWeapon: true);
            AddSoulShopItem(grid, "Shotgun", "Weapon: Shotgun", "1 use/run vs Tyler (Center). Triggers permanent Nightmare Tyler.", 2, isWeapon: true);
            AddSoulShopItem(grid, "Harpoon", "Weapon: Harpoon", "1 use/run vs Orville (Right). Triggers permanent Nightmare Orville.", 2, isWeapon: true);
            AddSoulShopItem(grid, "Spiked Mace", "Weapon: Spiked Mace", "1 use/run vs Sandy (Rear). Triggers permanent Nightmare Sandy.", 2, isWeapon: true);
        }

        private void AddSoulShopItem(Transform parent, string itemId, string title, string desc, int cost, bool isWeapon = false)
        {
            bool owned = MetaProgressionManager.HasInstance && MetaProgressionManager.Instance.HasUnlockedSoulItem(itemId);
            int souls = MetaProgressionManager.HasInstance ? MetaProgressionManager.Instance.SoulCoins : 0;

            var card = TylerlandUIKit.CreatePanel(parent, "SoulItem_" + itemId, TylerlandUIKit.CardBg, false);
            TylerlandUIKit.AddBorder(card.gameObject, owned ? TylerlandUIKit.AccentEmerald : TylerlandUIKit.AccentPurple, 1.5f);

            var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 12, 12);
            vlg.spacing = 6f;
            vlg.childForceExpandWidth = true;

            TylerlandUIKit.CreateText(card, title, 20, TylerlandUIKit.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            var d = TylerlandUIKit.CreateText(card, desc, 15, TylerlandUIKit.TextMuted, TextAnchor.UpperLeft);
            d.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            string btnLabel = owned ? "✓ UNLOCKED" : $"BUY (✦ {cost} SOUL COINS)";
            var btn = TylerlandUIKit.CreateButton(card, btnLabel, owned ? TylerlandUIKit.AccentEmerald : TylerlandUIKit.AccentPurple, () =>
            {
                if (!MetaProgressionManager.HasInstance) return;
                if (MetaProgressionManager.Instance.HasUnlockedSoulItem(itemId)) return;
                if (!MetaProgressionManager.Instance.SpendSoulCoins(cost))
                {
                    ShowFeedback($"Need ✦ {cost} Soul Coins for {title}!", isError: true);
                    return;
                }
                MetaProgressionManager.Instance.UnlockSoulItem(itemId);
                if (isWeapon && RunManager.HasInstance && !RunManager.Instance.State.HasUsedWeaponThisRun(itemId) && !RunManager.Instance.State.weaponsOwned.Contains(itemId))
                {
                    RunManager.Instance.AddWeapon(itemId);
                }
                if (DoorSystemManager.HasInstance) DoorSystemManager.Instance.ApplyUpgradesToAllDoors();
                AudioSettingsManager.PlayTestTone(false);
                OpenSubView(HubSubView.SoulCoinShop);
                ShowFeedback($"Permanently unlocked {title}!");
            }, 42f, 17);
            btn.interactable = !owned && souls >= cost;
        }

        private void BuildRunShop()
        {
            var grid = CreateShopGrid(_contentContainer);

            // Emergency Repair (repeatable)
            AddRepairShopItem(grid, "Emergency Door Repair", "Restores ALL 4 doors (including Broken shutters) to 100% Durability & Energy.", 80);

            // Tiered Run Upgrades (wired directly to DoorController.ApplyUpgradeModifiers)
            AddRunUpgradeTiered(grid, "Door Battery", "DOOR_BATTERY", 3, new[] { 100, 160, 220 }, "+20% / +40% / +60% Max Door Energy for this run.");
            AddRunUpgradeTiered(grid, "Reinforced Plating", "REINFORCED_PLATING", 3, new[] { 100, 160, 220 }, "+25% / +50% / +75% Max Door Durability for this run.");
            AddRunUpgradeTiered(grid, "Efficient Motor", "EFFICIENT_MOTOR", 2, new[] { 120, 200 }, "-20% / -40% Door Power Drain while closed.");
            AddRunUpgradeTiered(grid, "Quick Slam Hydraulics", "QUICK_SLAM", 2, new[] { 110, 180 }, "Faster door slam speed (0.20s -> 0.16s).");
            AddRunUpgradeTiered(grid, "Fast Recharge Coils", "FAST_RECHARGE", 2, new[] { 130, 200 }, "+25% / +50% Door recharge efficiency.");
        }

        private void AddRepairShopItem(Transform parent, string title, string desc, int cost)
        {
            int cash = RunManager.HasInstance ? RunManager.Instance.Money : 0;
            var card = TylerlandUIKit.CreatePanel(parent, "RunItem_Repair", TylerlandUIKit.CardBg, false);
            TylerlandUIKit.AddBorder(card.gameObject, TylerlandUIKit.AccentGold, 1.5f);

            var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 12, 12);
            vlg.spacing = 6f;
            vlg.childForceExpandWidth = true;

            TylerlandUIKit.CreateText(card, title, 20, TylerlandUIKit.AccentGold, TextAnchor.MiddleLeft, FontStyle.Bold);
            var d = TylerlandUIKit.CreateText(card, desc, 15, TylerlandUIKit.TextMuted, TextAnchor.UpperLeft);
            d.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var btn = TylerlandUIKit.CreateButton(card, $"REPAIR ALL DOORS (${cost})", TylerlandUIKit.AccentGold, () =>
            {
                if (!RunManager.HasInstance) return;
                if (!RunManager.Instance.SpendMoney(cost))
                {
                    ShowFeedback($"Not enough cash! Need ${cost}.", isError: true);
                    return;
                }
                if (DoorSystemManager.HasInstance) DoorSystemManager.Instance.RepairAllDoors();
                AudioSettingsManager.PlayTestTone(false);
                OpenSubView(HubSubView.RunShop);
                ShowFeedback("All 4 office doors repaired to 100%!");
            }, 42f, 17);
            btn.interactable = cash >= cost;
        }

        private void AddRunUpgradeTiered(Transform parent, string title, string prefix, int maxTier, int[] costs, string desc)
        {
            int ownedTier = 0;
            if (RunManager.HasInstance)
            {
                for (int t = maxTier; t >= 1; t--)
                {
                    if (RunManager.Instance.HasUpgrade($"{prefix}_{t}"))
                    {
                        ownedTier = t;
                        break;
                    }
                }
            }

            bool maxed = ownedTier >= maxTier;
            int nextTier = ownedTier + 1;
            int cost = maxed ? 0 : costs[Mathf.Clamp(nextTier - 1, 0, costs.Length - 1)];
            int cash = RunManager.HasInstance ? RunManager.Instance.Money : 0;

            var card = TylerlandUIKit.CreatePanel(parent, "RunItem_" + prefix, TylerlandUIKit.CardBg, false);
            TylerlandUIKit.AddBorder(card.gameObject, maxed ? TylerlandUIKit.AccentEmerald : TylerlandUIKit.AccentBlue, 1.5f);

            var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 12, 12);
            vlg.spacing = 6f;
            vlg.childForceExpandWidth = true;

            TylerlandUIKit.CreateText(card, $"{title}  [Tier {ownedTier}/{maxTier}]", 20, TylerlandUIKit.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            var d = TylerlandUIKit.CreateText(card, desc, 15, TylerlandUIKit.TextMuted, TextAnchor.UpperLeft);
            d.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            string btnLabel = maxed ? "✓ MAX TIER" : $"UPGRADE TIER {nextTier} (${cost})";
            var btn = TylerlandUIKit.CreateButton(card, btnLabel, maxed ? TylerlandUIKit.AccentEmerald : TylerlandUIKit.AccentBlue, () =>
            {
                if (!RunManager.HasInstance || maxed) return;
                if (!RunManager.Instance.SpendMoney(cost))
                {
                    ShowFeedback($"Not enough cash! Need ${cost}.", isError: true);
                    return;
                }
                RunManager.Instance.AddUpgrade($"{prefix}_{nextTier}");
                if (DoorSystemManager.HasInstance) DoorSystemManager.Instance.ApplyUpgradesToAllDoors();
                AudioSettingsManager.PlayTestTone(false);
                OpenSubView(HubSubView.RunShop);
                ShowFeedback($"Installed {title} Tier {nextTier}!");
            }, 42f, 17);
            btn.interactable = !maxed && cash >= cost;
        }

        private Transform CreateShopGrid(Transform parent)
        {
            var gridObj = new GameObject("ShopGrid", typeof(RectTransform));
            gridObj.transform.SetParent(parent, false);
            var grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(365f, 185f);
            grid.spacing = new Vector2(18f, 18f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            var le = gridObj.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;
            return gridObj.transform;
        }

        #region Between-Nights Minigame (Signal Calibration)
        private void BuildMinigameView()
        {
            _mgStatusText = TylerlandUIKit.CreateText(_contentContainer, "Press START CALIBRATION to begin (20 seconds)!", 22, TylerlandUIKit.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            _mgStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

            var gridHolder = new GameObject("MinigameGridHolder", typeof(RectTransform));
            gridHolder.transform.SetParent(_contentContainer, false);
            var glg = gridHolder.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(150f, 110f);
            glg.spacing = new Vector2(16f, 16f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.MiddleCenter;
            var le = gridHolder.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;

            for (int i = 0; i < 9; i++)
            {
                int idx = i;
                var btn = TylerlandUIKit.CreateButton(gridHolder.transform, $"NODE {i + 1}", TylerlandUIKit.BtnNeutral, () => OnMinigameNodeClicked(idx), 110f, 20);
                _mgGridButtons[i] = btn;
                _mgGridImages[i] = btn.GetComponent<Image>();
                _mgGridLabels[i] = btn.GetComponentInChildren<Text>();
            }

            StartMinigame();
        }

        private void StartMinigame()
        {
            _mgRunning = true;
            _mgTimeLeft = 20f;
            _mgScore = 0;
            _mgMisses = 0;
            _mgActiveIndex = -1;
            _mgSpawnTimer = 0.25f;
            ResetMinigameGridVisuals();
        }

        private void ResetMinigameGridVisuals()
        {
            for (int i = 0; i < 9; i++)
            {
                if (_mgGridButtons[i] == null) continue;
                var c = _mgGridButtons[i].colors;
                c.normalColor = TylerlandUIKit.BtnNeutral;
                c.highlightedColor = new Color(0.22f, 0.24f, 0.32f, 1f);
                _mgGridButtons[i].colors = c;
                if (_mgGridLabels[i] != null) _mgGridLabels[i].text = $"NODE {i + 1}";
            }
        }

        private void SpawnNextMinigameNode()
        {
            ResetMinigameGridVisuals();
            int next = Random.Range(0, 9);
            if (next == _mgActiveIndex) next = (next + 1) % 9;
            _mgActiveIndex = next;
            _mgIsGlitchTrap = Random.value < 0.24f;
            _mgSpawnTimer = _mgIsGlitchTrap ? 0.95f : 1.15f;

            if (_mgGridButtons[next] != null)
            {
                Color nodeCol = _mgIsGlitchTrap ? TylerlandUIKit.AccentCrimson : TylerlandUIKit.AccentEmerald;
                var c = _mgGridButtons[next].colors;
                c.normalColor = nodeCol;
                c.highlightedColor = Color.Lerp(nodeCol, Color.white, 0.2f);
                _mgGridButtons[next].colors = c;
                if (_mgGridLabels[next] != null)
                {
                    _mgGridLabels[next].text = _mgIsGlitchTrap ? "⚠ GLITCH!\nDO NOT CLICK" : "⚡ SIGNAL!\nCLICK NOW";
                }
            }
        }

        private void OnMinigameNodeClicked(int index)
        {
            if (!_mgRunning) return;

            if (index == _mgActiveIndex)
            {
                if (_mgIsGlitchTrap)
                {
                    _mgMisses++;
                    _mgScore = Mathf.Max(0, _mgScore - 2);
                    ShowFeedback("⚠ NIGHTMARE GLITCH HIT! (-2 points)", isError: true);
                }
                else
                {
                    _mgScore++;
                    AudioSettingsManager.PlayTestTone(false);
                    ShowFeedback($"⚡ Signal calibrated! (+1 -> {_mgScore})");
                }
                _mgActiveIndex = -1;
                SpawnNextMinigameNode();
            }
        }

        private void FinishMinigame()
        {
            _mgRunning = false;
            _minigamePlayedThisTransition = true;
            ResetMinigameGridVisuals();

            int cashReward = Mathf.Clamp(_mgScore * 12, 20, 180);
            int soulReward = _mgScore >= 8 ? 1 : 0;

            if (RunManager.HasInstance) RunManager.Instance.AddMoney(cashReward);
            if (soulReward > 0 && MetaProgressionManager.HasInstance)
            {
                MetaProgressionManager.Instance.AddSoulCoins(soulReward);
                MetaProgressionManager.Instance.SaveToDisk();
            }

            string resultMsg = soulReward > 0
                ? $"★ CALIBRATION COMPLETE! Score: {_mgScore} -> Earned +${cashReward} Cash & +1 Soul Coin (✦)!"
                : $"CALIBRATION COMPLETE! Score: {_mgScore} -> Earned +${cashReward} Cash! (Score 8+ for a Soul Coin)";

            OpenSubView(HubSubView.Hub);
            ShowFeedback(resultMsg);
        }
        #endregion

        private void BuildVictoryView()
        {
            int money = RunManager.HasInstance ? RunManager.Instance.Money : 0;
            int souls = MetaProgressionManager.HasInstance ? MetaProgressionManager.Instance.SoulCoins : 0;

            var summary = TylerlandUIKit.CreateText(
                _contentContainer,
                $"Final Shift Cash: <color=#4ade80>${money}</color>\nPersistent Soul Coins: <color=#c084fc>✦ {souls}</color>\n\nVisit the Soul Coin Shop to spend your earned Soul Coins or return to the Main Menu to start a fresh 5-night cycle.",
                24,
                TylerlandUIKit.TextPrimary,
                TextAnchor.MiddleCenter);
            summary.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var btnRow = new GameObject("VictoryButtons", typeof(RectTransform));
            btnRow.transform.SetParent(_contentContainer, false);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24f;
            hlg.childForceExpandWidth = true;
            btnRow.AddComponent<LayoutElement>().preferredHeight = 62f;

            TylerlandUIKit.CreateButton(btnRow.transform, "✦ OPEN SOUL COIN SHOP", TylerlandUIKit.AccentPurple, () => OpenSubView(HubSubView.SoulCoinShop), 60f, 22);
            TylerlandUIKit.CreateButton(btnRow.transform, "🏠 RETURN TO MAIN MENU", TylerlandUIKit.AccentEmerald, () =>
            {
                if (GameFlowManager.HasInstance) GameFlowManager.Instance.ReturnToMainMenuScene();
            }, 60f, 22);
        }

        private void BuildGameOverView()
        {
            int night = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;
            int souls = MetaProgressionManager.HasInstance ? MetaProgressionManager.Instance.SoulCoins : 0;

            var summary = TylerlandUIKit.CreateText(
                _contentContainer,
                $"Killed during <b>Night {night}</b>.\n\nYour <color=#c084fc>✦ {souls} Soul Coins</color> and permanent unlocks remain safe.\nAll guns restore to 1 use on your next 5-night cycle.",
                24,
                TylerlandUIKit.TextPrimary,
                TextAnchor.MiddleCenter);
            summary.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var btnRow = new GameObject("GameOverButtons", typeof(RectTransform));
            btnRow.transform.SetParent(_contentContainer, false);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24f;
            hlg.childForceExpandWidth = true;
            btnRow.AddComponent<LayoutElement>().preferredHeight = 62f;

            TylerlandUIKit.CreateButton(btnRow.transform, "🔄 START FRESH 5-NIGHT RUN", TylerlandUIKit.AccentCrimson, () =>
            {
                if (GameFlowManager.HasInstance) GameFlowManager.Instance.BeginGameplayFromMenu(newRun: true, targetNight: 1);
            }, 60f, 22);

            TylerlandUIKit.CreateButton(btnRow.transform, "🏠 RETURN TO MAIN MENU", TylerlandUIKit.BtnNeutral, () =>
            {
                if (GameFlowManager.HasInstance) GameFlowManager.Instance.ReturnToMainMenuScene();
            }, 60f, 22);
        }

        private void OnStartNextNightClicked()
        {
            if (RunManager.HasInstance)
            {
                if (RunManager.Instance.CurrentNight >= RunManager.Instance.TotalNights)
                {
                    if (GameFlowManager.HasInstance) GameFlowManager.Instance.ReturnToMainMenuScene();
                }
                else
                {
                    RunManager.Instance.AdvanceToNextNight();
                }
            }
            else if (GameFlowManager.HasInstance)
            {
                GameFlowManager.Instance.ReturnToMainMenuScene();
            }
        }
    }
}
