#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class aaaaSBVCfbS700 : Strategy
    {
        // Variables private de l'indicateur original
        private int consecutiveBarsInValueArea = 0;
        private Swing swingIndicator;
        private double lastSwingHigh = 0;
        private int lastSwingHighBar = -1;
        private bool swingHighBroken = false;
        private int offsetTicksVwap = 2;
        private bool firstBreakoutDownUpStd2Triggered = false;
        
        // Variables pour le signal firstBuy
        private bool firstBuyTriggered = false;
        
        // Variables pour le signal firstbreakoutdownup
        private bool firstBreakoutDownUpTriggered = false;
        
        // Variables pour le swing low de la vague
        private double currentWaveSwingLow = 0;
        private int currentWaveSwingLowBar = -1;
        private double waveSize = 0;
        
        // Variables pour le VWAP Ancré
        private int vwapAnchorBarIndex = -1;
        private bool vwapSequenceActive = false;
        
        // Variables pour la gestion des positions
        private bool barreBreakPositionOpen = false;
        private bool firstBuyPositionOpen = false;
        private bool fbduPositionOpen = false;
        private bool fbdu2PositionOpen = false;
        
        // Variables pour stocker les niveaux de trading
        private double barreBreakEntryPrice = 0;
        private double firstBuyEntryPrice = 0;
        private double fbduEntryPrice = 0;
        private double fbdu2EntryPrice = 0;
        
        // Variables pour gérer les positions partielles
        private int bbContractsRemaining = 0;
        private int fbContractsRemaining = 0;
        private int fbduContractsRemaining = 0;
        private int fbdu2ContractsRemaining = 0;
        
        // Paramètres
        private int strength = 3;
        private int lookBackPeriod = 50;
        private int offsetTicks = 2;
        private bool paintBars = true;
		
		// BREAK-EVEN VARIABLES pour tous les signaux
		// BarreBreak
		private bool bbBreakEvenSet = false;
		private double bbBreakEvenPrice = 0;
		
		// FirstBuy
		private bool fbBreakEvenSet = false;
		private double fbBreakEvenPrice = 0;
		
		// FirstBreakoutDownUp (déjà existant)
		private bool fbduBreakEvenSet = false;
		private double fbduBreakEvenPrice = 0;
		
		// FirstBreakoutDownUpStd2
		private bool fbdu2BreakEvenSet = false;
		private double fbdu2BreakEvenPrice = 0;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Stratégie basée sur l'indicateur aaaSBVCfb avec gestion avancée des 4 signaux et Break-Even";
                Name = "aaaaSBVCfbS700";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 4;
                EntryHandling = EntryHandling.UniqueEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                IsInstantiatedOnEachOptimizationIteration = false;
                
                // Configuration pour affichage
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                
                // Valeurs par défaut des paramètres de l'indicateur
                Strength = 5;
                LookBackPeriod = 50;
                OffsetTicks = 2;
                PaintBars = true;
                ShowSwingLowMarker = true;
                
                // PARAMÈTRES DE VALIDATION DE CASSURE
                MinimumSwingDistanceTicks = 30;
                EnableMinimumDistance = true;
                
                // Qualité de la montée
                EnableUpBarFilter = true;
                MaxDownBarsAllowed = 4;
                
                // Nombre de barres total
                EnableBarCountFilter = true;
                MinBarsRequired = 4;
                MaxBarsAllowed = 10;
                
                ShowRejectedBreakouts = true;
                
                // Barre de cassure
                EnableBreakoutBarFilter = true;
                MinBreakoutBarSizeTicks = 2;
                RequireCloseAbovePreHigh = true;
                
                // Couleurs par défaut
                UpBreakoutColor = Brushes.Lime;
                SwingHighLineColor = Brushes.DodgerBlue;
                SwingLowColor = Brushes.Orange;
                RejectedBreakoutColor = Brushes.Gray;
                MarkerSize = 10;
                LineWidth = 2;
                ShowSwingHighLine = true;
                
                // Valeurs par défaut pour le VWAP
                ShowVwap = true;
                StdDev1Multiplier = 1.0;
                StdDev2Multiplier = 2.0;
                VwapColor = Brushes.Cyan;
                StdDev1BandColor = Brushes.LightGray;
                StdDev2BandColor = Brushes.DarkGray;
                VwapLineWidth = 2;
                
                // Valeurs par défaut pour le CWAP
                ShowCwap = false;
                CwapColor = Brushes.Magenta;
                CwapStdDev1BandColor = Brushes.LightPink;
                CwapStdDev2BandColor = Brushes.HotPink;
                CwapLineWidth = 2;
                
                OffsetTicksVwap = 2;
                ShowVwapBreakMarker = true;
                VwapBreakColor = Brushes.Red;
                EnableValueAreaExit = true;
                BarsInValueAreaForExit = 3;
                
                // Paramètres pour BarreBreak Signal
                ShowBarreBreakSignal = true;
                BarreBreakTargetTicks = 6;
                BarreBreakTarget2Ticks = 12;
                BarreBreakStopLossTicks = 8;
                BarreBreakMarkerColor = Brushes.Yellow;
                BarreBreakTextColor = Brushes.Black;
                BarreBreakContracts = 2;
                BarreBreakTarget1Contracts = 1;
                
                // Premier filtre horaire BB
                BarreBreakEnableTimeFilter = false;
                BarreBreakStartTime = DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture);
                BarreBreakEndTime = DateTime.Parse("11:00", System.Globalization.CultureInfo.InvariantCulture);
                
                // Deuxième filtre horaire BB
                BarreBreakEnableTimeFilter2 = false;
                BarreBreakStartTime2 = DateTime.Parse("14:00", System.Globalization.CultureInfo.InvariantCulture);
                BarreBreakEndTime2 = DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);
                
                BarreBreakUseTrailStop = false;
                BarreBreakTrailStopDistance = 10;
                BarreBreakUseParabolicStop = false;
                BarreBreakParabolicAcceleration = 0.02;
                BarreBreakParabolicAccelerationMax = 0.2;
                BarreBreakParabolicAccelerationStep = 0.02;
                BarreBreakUseBreakEven = true;
                BarreBreakBreakEvenOffset = 2;
                
                // Paramètres pour firstBuy Signal
                ShowFirstBuySignal = true;
                FirstBuyOffsetTicks = 1;
                FirstBuyMarkerColor = Brushes.Gold;
                FirstBuyTextColor = Brushes.White;
                FirstBuyTargetTicks = 5;
                FirstBuyTarget2Ticks = 10;
                FirstBuyStopLossTicks = 10;
                FirstBuyContracts = 2;
                FirstBuyTarget1Contracts = 1;
                
                // Premier filtre horaire FB
                FirstBuyEnableTimeFilter = false;
                FirstBuyStartTime = DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture);
                FirstBuyEndTime = DateTime.Parse("11:00", System.Globalization.CultureInfo.InvariantCulture);
                
                // Deuxième filtre horaire FB
                FirstBuyEnableTimeFilter2 = false;
                FirstBuyStartTime2 = DateTime.Parse("14:00", System.Globalization.CultureInfo.InvariantCulture);
                FirstBuyEndTime2 = DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);
                
                FirstBuyUseTrailStop = false;
                FirstBuyTrailStopDistance = 10;
                FirstBuyUseParabolicStop = false;
                FirstBuyParabolicAcceleration = 0.02;
                FirstBuyParabolicAccelerationMax = 0.2;
                FirstBuyParabolicAccelerationStep = 0.02;
                FirstBuyUseBreakEven = true;
                FirstBuyBreakEvenOffset = 2;
                
                // Paramètres pour firstbreakoutdownup
                ShowFirstBreakoutDownUpSignal = true;
                FirstBreakoutDownUpZoneOffsetTicks = 2;
                RequireCloseAboveStdDev1 = false;
                FirstBreakoutDownUpMarkerColor = Brushes.Aqua;
                FirstBreakoutDownUpTextColor = Brushes.White;
                FirstBreakoutDownUpTargetTicks = 5;
                FirstBreakoutDownUpTarget2Ticks = 10;
                FirstBreakoutDownUpStopLossTicks = 10;
                FirstBreakoutDownUpContracts = 2;
                FirstBreakoutDownUpTarget1Contracts = 1;
                
                // Premier filtre horaire FBDU
                FirstBreakoutDownUpEnableTimeFilter = false;
                FirstBreakoutDownUpStartTime = DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture);
                FirstBreakoutDownUpEndTime = DateTime.Parse("11:00", System.Globalization.CultureInfo.InvariantCulture);
                
                // Deuxième filtre horaire FBDU
                FirstBreakoutDownUpEnableTimeFilter2 = false;
                FirstBreakoutDownUpStartTime2 = DateTime.Parse("14:00", System.Globalization.CultureInfo.InvariantCulture);
                FirstBreakoutDownUpEndTime2 = DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);
                
                FirstBreakoutDownUpUseTrailStop = false;
                FirstBreakoutDownUpTrailStopDistance = 10;
                FirstBreakoutDownUpUseParabolicStop = false;
                FirstBreakoutDownUpParabolicAcceleration = 0.02;
                FirstBreakoutDownUpParabolicAccelerationMax = 0.2;
                FirstBreakoutDownUpParabolicAccelerationStep = 0.02;
				FirstBreakoutDownUpUseBreakEven = true;
				FirstBreakoutDownUpBreakEvenOffset = 2;
                
                // Paramètres pour FirstBreakoutDownUpStd2
                ShowFirstBreakoutDownUpStd2Signal = true;
                RequireCloseAboveStdDev2 = false;
                FirstBreakoutDownUpStd2MarkerColor = Brushes.Purple;
                FirstBreakoutDownUpStd2TextColor = Brushes.White;
                FirstBreakoutDownUpStd2TargetTicks = 8;
                FirstBreakoutDownUpStd2Target2Ticks = 16;
                FirstBreakoutDownUpStd2StopLossTicks = 12;
                FirstBreakoutDownUpStd2Contracts = 2;
                FirstBreakoutDownUpStd2Target1Contracts = 1;
                
                // Premier filtre horaire FBDU2
                FirstBreakoutDownUpStd2EnableTimeFilter = false;
                FirstBreakoutDownUpStd2StartTime = DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture);
                FirstBreakoutDownUpStd2EndTime = DateTime.Parse("11:00", System.Globalization.CultureInfo.InvariantCulture);
                
                // Deuxième filtre horaire FBDU2
                FirstBreakoutDownUpStd2EnableTimeFilter2 = false;
                FirstBreakoutDownUpStd2StartTime2 = DateTime.Parse("14:00", System.Globalization.CultureInfo.InvariantCulture);
                FirstBreakoutDownUpStd2EndTime2 = DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);
                
                FirstBreakoutDownUpStd2UseTrailStop = false;
                FirstBreakoutDownUpStd2TrailStopDistance = 10;
                FirstBreakoutDownUpStd2UseParabolicStop = false;
                FirstBreakoutDownUpStd2ParabolicAcceleration = 0.02;
                FirstBreakoutDownUpStd2ParabolicAccelerationMax = 0.2;
                FirstBreakoutDownUpStd2ParabolicAccelerationStep = 0.02;
                FirstBreakoutDownUpStd2UseBreakEven = true;
                FirstBreakoutDownUpStd2BreakEvenOffset = 2;
                
                // Paramètres de gestion des sorties
                ClosePositionsOnVwapBreak = true;
                ClosePositionsOnVAConsolidation = true;
                
                // Plots pour affichage des valeurs VWAP/CWAP
                AddPlot(new Stroke(Brushes.Transparent, 0), PlotStyle.Line, "CurrentSwingHigh");
                AddPlot(new Stroke(Brushes.Transparent, 0), PlotStyle.Line, "BreakoutLevel");
                AddPlot(new Stroke(Brushes.Transparent, 0), PlotStyle.Line, "WaveSwingLow");
                AddPlot(new Stroke(Brushes.Transparent, 0), PlotStyle.Line, "WaveSizeTicks");
                
                // Plots pour le VWAP et les écarts-types
                AddPlot(new Stroke(VwapColor, VwapLineWidth), PlotStyle.Line, "VWAP");
                AddPlot(new Stroke(StdDev1BandColor, 1), PlotStyle.Line, "StdDev +1");
                AddPlot(new Stroke(StdDev1BandColor, 1), PlotStyle.Line, "StdDev -1");
                AddPlot(new Stroke(StdDev2BandColor, 1), PlotStyle.Line, "StdDev +2");
                AddPlot(new Stroke(StdDev2BandColor, 1), PlotStyle.Line, "StdDev -2");
                
                // Plots pour le CWAP et les écarts-types
                AddPlot(new Stroke(CwapColor, CwapLineWidth), PlotStyle.Line, "CWAP");
                AddPlot(new Stroke(CwapStdDev1BandColor, 1), PlotStyle.Line, "CWAP StdDev +1");
                AddPlot(new Stroke(CwapStdDev1BandColor, 1), PlotStyle.Line, "CWAP StdDev -1");
                AddPlot(new Stroke(CwapStdDev2BandColor, 1), PlotStyle.Line, "CWAP StdDev +2");
                AddPlot(new Stroke(CwapStdDev2BandColor, 1), PlotStyle.Line, "CWAP StdDev -2");
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                swingIndicator = Swing(Strength);
            }
        }
        
        // Méthode helper modifiée pour vérifier les DEUX filtres horaires pour chaque signal
        private bool IsWithinTradingHours(string signalType)
        {
            TimeSpan currentTime = Time[0].TimeOfDay;
            TimeSpan startTime1, endTime1, startTime2, endTime2;
            bool enableFilter1 = false;
            bool enableFilter2 = false;
            
            switch (signalType)
            {
                case "BB":
                    enableFilter1 = BarreBreakEnableTimeFilter;
                    startTime1 = BarreBreakStartTime.TimeOfDay;
                    endTime1 = BarreBreakEndTime.TimeOfDay;
                    enableFilter2 = BarreBreakEnableTimeFilter2;
                    startTime2 = BarreBreakStartTime2.TimeOfDay;
                    endTime2 = BarreBreakEndTime2.TimeOfDay;
                    break;
                    
                case "FB":
                    enableFilter1 = FirstBuyEnableTimeFilter;
                    startTime1 = FirstBuyStartTime.TimeOfDay;
                    endTime1 = FirstBuyEndTime.TimeOfDay;
                    enableFilter2 = FirstBuyEnableTimeFilter2;
                    startTime2 = FirstBuyStartTime2.TimeOfDay;
                    endTime2 = FirstBuyEndTime2.TimeOfDay;
                    break;
                    
                case "FBDU":
                    enableFilter1 = FirstBreakoutDownUpEnableTimeFilter;
                    startTime1 = FirstBreakoutDownUpStartTime.TimeOfDay;
                    endTime1 = FirstBreakoutDownUpEndTime.TimeOfDay;
                    enableFilter2 = FirstBreakoutDownUpEnableTimeFilter2;
                    startTime2 = FirstBreakoutDownUpStartTime2.TimeOfDay;
                    endTime2 = FirstBreakoutDownUpEndTime2.TimeOfDay;
                    break;
                    
                case "FBDU2":
                    enableFilter1 = FirstBreakoutDownUpStd2EnableTimeFilter;
                    startTime1 = FirstBreakoutDownUpStd2StartTime.TimeOfDay;
                    endTime1 = FirstBreakoutDownUpStd2EndTime.TimeOfDay;
                    enableFilter2 = FirstBreakoutDownUpStd2EnableTimeFilter2;
                    startTime2 = FirstBreakoutDownUpStd2StartTime2.TimeOfDay;
                    endTime2 = FirstBreakoutDownUpStd2EndTime2.TimeOfDay;
                    break;
                    
                default:
                    return true;
            }
            
            // Si aucun filtre n'est activé, on peut trader
            if (!enableFilter1 && !enableFilter2)
                return true;
            
            bool inTimeRange1 = false;
            bool inTimeRange2 = false;
            
            // Vérifier la première plage horaire si activée
            if (enableFilter1)
            {
                inTimeRange1 = currentTime >= startTime1 && currentTime <= endTime1;
            }
            
            // Vérifier la deuxième plage horaire si activée
            if (enableFilter2)
            {
                inTimeRange2 = currentTime >= startTime2 && currentTime <= endTime2;
            }
            
            // On peut trader si on est dans l'une ou l'autre plage horaire active
            if (enableFilter1 && enableFilter2)
                return inTimeRange1 || inTimeRange2;
            else if (enableFilter1)
                return inTimeRange1;
            else if (enableFilter2)
                return inTimeRange2;
            else
                return true;
        }
        
        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(Strength * 2, LookBackPeriod))
                return;
            
            try
            {
                // Réinitialisation à chaque nouvelle session
                if (Bars.IsFirstBarOfSession)
                {
                    lastSwingHigh = 0;
                    lastSwingHighBar = -1;
                    swingHighBroken = false;
                    currentWaveSwingLow = 0;
                    currentWaveSwingLowBar = -1;
                    waveSize = 0;
                    vwapAnchorBarIndex = -1;
                    vwapSequenceActive = false;
                    consecutiveBarsInValueArea = 0;
                    firstBuyTriggered = false;
                    firstBreakoutDownUpTriggered = false;
                    firstBreakoutDownUpStd2Triggered = false;
                    
                    if (Position.MarketPosition != MarketPosition.Flat)
                    {
                        ExitLong();
                    }
                    
                    barreBreakPositionOpen = false;
                    firstBuyPositionOpen = false;
                    fbduPositionOpen = false;
                    fbdu2PositionOpen = false;
                    
                    bbContractsRemaining = 0;
                    fbContractsRemaining = 0;
                    fbduContractsRemaining = 0;
                    fbdu2ContractsRemaining = 0;
					
					// Réinitialisation de tous les break-evens
					bbBreakEvenSet = false;
					bbBreakEvenPrice = 0;
					fbBreakEvenSet = false;
					fbBreakEvenPrice = 0;
					fbduBreakEvenSet = false;
					fbduBreakEvenPrice = 0;
					fbdu2BreakEvenSet = false;
					fbdu2BreakEvenPrice = 0;
                    
                    VWAP[0] = double.NaN;
                    StdDevP1[0] = double.NaN;
                    StdDevM1[0] = double.NaN;
                    StdDevP2[0] = double.NaN;
                    StdDevM2[0] = double.NaN;
                    
                    CWAP[0] = double.NaN;
                    CwapStdDevP1[0] = double.NaN;
                    CwapStdDevM1[0] = double.NaN;
                    CwapStdDevP2[0] = double.NaN;
                    CwapStdDevM2[0] = double.NaN;
                    
                    Print($"Nouvelle session détectée - Réinitialisation des variables à {Time[0]}");
                }
                
                // VÉRIFIER LA CASSURE DU VWAP À LA BAISSE
                if (vwapSequenceActive && ShowVwap && CurrentBar > 0)
                {
                    if (!double.IsNaN(VWAP[1]))
                    {
                        double vwapBreakLevel = VWAP[1] - (OffsetTicksVwap * TickSize);
                        
                        if (Close[0] < vwapBreakLevel)
                        {
                            Print($"VWAP cassé à la baisse à {Time[0]} - Fin de la séquence");
                            
                            if (ShowVwapBreakMarker)
                            {
                                Draw.ArrowDown(this, "VwapBreak_" + CurrentBar, false, 0, High[0] + (2 * TickSize), VwapBreakColor);
                                Draw.Text(this, "VwapBreakText_" + CurrentBar, "VWAP Break", 0, High[0] + (5 * TickSize), VwapBreakColor);
                            }
                            
                            if (ClosePositionsOnVwapBreak)
                            {
                                CloseAllPositions("VWAP Break");
                            }
                            
                            EndVwapSequence();
                            return;
                        }
                        
                        // Condition 2: Barres consécutives dans la Value Area
                        if (EnableValueAreaExit && !double.IsNaN(StdDevM1[1]) && !double.IsNaN(StdDevP1[1]))
                        {
                            bool openInValueArea = Open[0] >= StdDevM1[1] && Open[0] <= StdDevP1[1];
                            bool closeInValueArea = Close[0] >= StdDevM1[1] && Close[0] <= StdDevP1[1];
                            
                            if (openInValueArea && closeInValueArea)
                            {
                                consecutiveBarsInValueArea++;
                                
                                if (consecutiveBarsInValueArea >= BarsInValueAreaForExit)
                                {
                                    Print($"Consolidation détectée dans Value Area à {Time[0]} - Fin de la séquence");
                                    
                                    if (ShowVwapBreakMarker)
                                    {
                                        Draw.Square(this, "VAConsolidation_" + CurrentBar, false, 0, (StdDevP1[1] + StdDevM1[1]) / 2, Brushes.Yellow);
                                        Draw.Text(this, "VAConsolidationText_" + CurrentBar, "VA Exit", 0, StdDevP1[1] + (3 * TickSize), Brushes.Yellow);
                                    }
                                    
                                    if (ClosePositionsOnVAConsolidation)
                                    {
                                        CloseAllPositions("VA Consolidation");
                                    }
                                    
                                    EndVwapSequence();
                                    return;
                                }
                            }
                            else
                            {
                                consecutiveBarsInValueArea = 0;
                            }
                        }
                    }
                }
                
                // SUITE DE LA LOGIQUE DE L'INDICATEUR
                int swingHighBarsAgo = swingIndicator.SwingHighBar(0, 1, LookBackPeriod);
                
                if (swingHighBarsAgo > 0)
                {
                    double currentSwingHigh = High[swingHighBarsAgo];
                    double breakoutLevel = currentSwingHigh + (OffsetTicks * TickSize);
                    
                    CurrentSwingHigh[0] = currentSwingHigh;
                    BreakoutLevel[0] = breakoutLevel;
                    
                    if (swingHighBarsAgo != lastSwingHighBar && !vwapSequenceActive)
                    {
                        lastSwingHigh = currentSwingHigh;
                        lastSwingHighBar = swingHighBarsAgo;
                        swingHighBroken = false;
                        
                        currentWaveSwingLow = 0;
                        currentWaveSwingLowBar = -1;
                        waveSize = 0;
                        
                        if (ShowSwingHighLine)
                        {
                            Draw.Line(this, "SwingHighLine_" + CurrentBar, false, swingHighBarsAgo, currentSwingHigh, 0, currentSwingHigh, SwingHighLineColor, DashStyleHelper.Dash, LineWidth);
                            Draw.Line(this, "BreakoutLine_" + CurrentBar, false, swingHighBarsAgo, breakoutLevel, 0, breakoutLevel, UpBreakoutColor, DashStyleHelper.Dot, 1);
                        }
                    }
                    
                    // Détecter une nouvelle cassure
                    if (Close[0] > breakoutLevel && !swingHighBroken && !vwapSequenceActive)
                    {
                        // VALIDATION DE LA BARRE DE CASSURE
                        bool breakoutBarValid = true;
                        
                        if (EnableBreakoutBarFilter)
                        {
                            bool isDoji = (Close[0] == Open[0]);
                            if (isDoji)
                            {
                                breakoutBarValid = false;
                                Print($"Cassure REJETÉE à {Time[0]} - Barre Doji détectée");
                                
                                if (ShowRejectedBreakouts)
                                {
                                    Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
                                    Draw.Text(this, "RejectedReason_" + CurrentBar, "Doji", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
                                }
                                return;
                            }
                            
                            double barSize = Math.Abs(Close[0] - Open[0]);
                            double barSizeTicks = Math.Round(barSize / TickSize);
                            
                            if (barSizeTicks < MinBreakoutBarSizeTicks)
                            {
                                breakoutBarValid = false;
                                Print($"Cassure REJETÉE à {Time[0]} - Barre trop petite: {barSizeTicks} < {MinBreakoutBarSizeTicks} ticks");
                                
                                if (ShowRejectedBreakouts)
                                {
                                    Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
                                    Draw.Text(this, "RejectedReason_" + CurrentBar, $"Size: {barSizeTicks}t<{MinBreakoutBarSizeTicks}t", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
                                }
                                return;
                            }
                            
                            if (RequireCloseAbovePreHigh && CurrentBar > 0)
                            {
                                if (Close[0] <= High[1])
                                {
                                    breakoutBarValid = false;
                                    Print($"Cassure REJETÉE à {Time[0]} - Close {Close[0]} <= High précédent {High[1]}");
                                    
                                    if (ShowRejectedBreakouts)
                                    {
                                        Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
                                        Draw.Text(this, "RejectedReason_" + CurrentBar, "Close≤PreHigh", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
                                    }
                                    return;
                                }
                            }
                        }
                        
                        if (!breakoutBarValid)
                            return;
                        
                        // Recherche du swing low pour validation
                        int searchStart = Math.Min(swingHighBarsAgo, LookBackPeriod);
                        double lowestPrice = double.MaxValue;
                        int lowestBar = -1;
                        
                        for (int i = 1; i <= swingHighBarsAgo; i++)
                        {
                            int swingLowBar = swingIndicator.SwingLowBar(0, i, searchStart);
                            if (swingLowBar > 0 && swingLowBar < swingHighBarsAgo)
                            {
                                lowestPrice = Low[swingLowBar];
                                lowestBar = swingLowBar;
                                break;
                            }
                        }
                        
                        if (lowestBar == -1)
                        {
                            for (int i = 0; i < swingHighBarsAgo; i++)
                            {
                                if (Low[i] < lowestPrice)
                                {
                                    lowestPrice = Low[i];
                                    lowestBar = i;
                                }
                            }
                        }
                        
                        // VALIDATION DE LA DISTANCE MINIMALE
                        bool distanceValid = true;
                        double distanceInTicks = 0;
                        
                        if (EnableMinimumDistance && lowestPrice > 0)
                        {
                            distanceInTicks = Math.Round((currentSwingHigh - lowestPrice) / TickSize);
                            
                            if (distanceInTicks < MinimumSwingDistanceTicks)
                            {
                                distanceValid = false;
                                Print($"Cassure REJETÉE à {Time[0]} - Distance insuffisante: {distanceInTicks} < {MinimumSwingDistanceTicks} ticks");
                                
                                if (ShowRejectedBreakouts)
                                {
                                    Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
                                    Draw.Text(this, "RejectedReason_" + CurrentBar, $"Dist: {distanceInTicks}t", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
                                }
                                return;
                            }
                        }
                        
                        // VALIDATION DE LA QUALITÉ DE LA MONTÉE
                        bool qualityValid = true;
                        int downBarsCount = 0;
                        
                        if (EnableUpBarFilter && lowestBar > 0)
                        {
                            for (int i = lowestBar - 1; i >= 0; i--)
                            {
                                if (Close[i] < Open[i])
                                {
                                    downBarsCount++;
                                }
                            }
                            
                            if (downBarsCount > MaxDownBarsAllowed)
                            {
                                qualityValid = false;
                                Print($"Cassure REJETÉE à {Time[0]} - Trop de barres down: {downBarsCount} > {MaxDownBarsAllowed}");
                                
                                if (ShowRejectedBreakouts)
                                {
                                    Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
                                    Draw.Text(this, "RejectedReason_" + CurrentBar, $"Down: {downBarsCount}/{MaxDownBarsAllowed}", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
                                }
                                return;
                            }
                        }
                        
                        // VALIDATION DU NOMBRE TOTAL DE BARRES
                        bool barCountValid = true;
                        int totalBarsCount = 0;
                        
                        if (EnableBarCountFilter && lowestBar > 0)
                        {
                            totalBarsCount = lowestBar; // Compte jusqu'à la barre actuelle
                            
                            if (totalBarsCount < MinBarsRequired)
                            {
                                barCountValid = false;
                                Print($"Cassure REJETÉE à {Time[0]} - Montée trop rapide: {totalBarsCount} < {MinBarsRequired} barres");
                                
                                if (ShowRejectedBreakouts)
                                {
                                    Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
                                    Draw.Text(this, "RejectedReason_" + CurrentBar, $"Bars: {totalBarsCount}<{MinBarsRequired}", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
                                }
                                return;
                            }
                            else if (totalBarsCount > MaxBarsAllowed)
                            {
                                barCountValid = false;
                                Print($"Cassure REJETÉE à {Time[0]} - Montée trop lente: {totalBarsCount} > {MaxBarsAllowed} barres");
                                
                                if (ShowRejectedBreakouts)
                                {
                                    Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
                                    Draw.Text(this, "RejectedReason_" + CurrentBar, $"Bars: {totalBarsCount}>{MaxBarsAllowed}", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
                                }
                                return;
                            }
                        }
                        
                        // CASSURE VALIDÉE
                        if (distanceValid && qualityValid && barCountValid)
                        {
                            swingHighBroken = true;
                            vwapSequenceActive = true;
                            consecutiveBarsInValueArea = 0;
                            firstBuyTriggered = false;
                            firstBreakoutDownUpTriggered = false;
                            firstBreakoutDownUpStd2Triggered = false;
                            
                            currentWaveSwingLow = lowestPrice;
                            currentWaveSwingLowBar = lowestBar;
                            
                            // SIGNAL BARREBREAK (BB) - AVEC FILTRE HORAIRE INDIVIDUEL
                            if (ShowBarreBreakSignal && !barreBreakPositionOpen && IsWithinTradingHours("BB"))
                            {
                                double entryPrice = Close[0];
                                double targetPrice = entryPrice + (BarreBreakTargetTicks * TickSize);
                                double target2Price = entryPrice + (BarreBreakTarget2Ticks * TickSize);
                                double stopLossPrice = entryPrice - (BarreBreakStopLossTicks * TickSize);
                                
                                bbContractsRemaining = BarreBreakContracts;
                                bbBreakEvenSet = false;
                                bbBreakEvenPrice = 0;
                                
								EnterLong(BarreBreakTarget1Contracts, "BB_T1");
								EnterLong(BarreBreakContracts - BarreBreakTarget1Contracts, "BB_T2");
                                barreBreakPositionOpen = true;
                                barreBreakEntryPrice = entryPrice;
                                
                                // Premier target partiel
								SetProfitTarget("BB_T1", CalculationMode.Price, targetPrice, false);
								SetProfitTarget("BB_T2", CalculationMode.Price, target2Price, false);
                                
                                // Stop loss pour les deux entrées
								if (BarreBreakUseTrailStop)
								{
									SetTrailStop("BB_T1", CalculationMode.Ticks, BarreBreakTrailStopDistance, false);
									SetTrailStop("BB_T2", CalculationMode.Ticks, BarreBreakTrailStopDistance, false);
								}
								else if (BarreBreakUseParabolicStop)
								{
									SetParabolicStop("BB_T1", CalculationMode.Price, stopLossPrice, false, 
										BarreBreakParabolicAcceleration, BarreBreakParabolicAccelerationMax, BarreBreakParabolicAccelerationStep);
									SetParabolicStop("BB_T2", CalculationMode.Price, stopLossPrice, false, 
										BarreBreakParabolicAcceleration, BarreBreakParabolicAccelerationMax, BarreBreakParabolicAccelerationStep);
								}
								else
								{
									SetStopLoss("BB_T1", CalculationMode.Price, stopLossPrice, false);
									SetStopLoss("BB_T2", CalculationMode.Price, stopLossPrice, false);
								}
                                
                                Print($"SIGNAL BARREBREAK EXÉCUTÉ à {Time[0]} - Entry: {entryPrice:F2}, T1: {targetPrice:F2}, T2: {target2Price:F2}, Stop: {stopLossPrice:F2}, Qty: {BarreBreakContracts}");
                                
                                Draw.ArrowUp(this, "BarreBreak_" + CurrentBar, false, 0, Low[0] - (2 * TickSize), BarreBreakMarkerColor);
                                Draw.Text(this, "BarreBreakText_" + CurrentBar, "BB", 0, Low[0] - (5 * TickSize), BarreBreakTextColor);
                                
                                Draw.Line(this, "BarreBreakTarget1_" + CurrentBar, false, 0, targetPrice, -5, targetPrice, Brushes.Lime, DashStyleHelper.Dot, 2);
                                Draw.Text(this, "BarreBreakTarget1Text_" + CurrentBar, $"T1: {targetPrice:F2} (+{BarreBreakTargetTicks}t)", -5, targetPrice, Brushes.Lime);
                                
                                Draw.Line(this, "BarreBreakTarget2_" + CurrentBar, false, 0, target2Price, -5, target2Price, Brushes.LimeGreen, DashStyleHelper.Dot, 2);
                                Draw.Text(this, "BarreBreakTarget2Text_" + CurrentBar, $"T2: {target2Price:F2} (+{BarreBreakTarget2Ticks}t)", -5, target2Price, Brushes.LimeGreen);
                                
                                Draw.Line(this, "BarreBreakStopLoss_" + CurrentBar, false, 0, stopLossPrice, -5, stopLossPrice, Brushes.OrangeRed, DashStyleHelper.Dot, 2);
                                Draw.Text(this, "BarreBreakStopLossText_" + CurrentBar, $"SL: {stopLossPrice:F2} (-{BarreBreakStopLossTicks}t)", -5, stopLossPrice, Brushes.OrangeRed);
                                
                                Draw.Dot(this, "BarreBreakEntry_" + CurrentBar, false, 0, entryPrice, Brushes.Yellow);
                            }
                            
                            if (currentWaveSwingLow > 0)
                            {
                                DateTime swingLowTime = Time[currentWaveSwingLowBar];
                                DateTime currentTime = Time[0];
                                
                                Print($"Cassure VALIDÉE à {Time[0]} - Distance: {distanceInTicks} ticks");
                                
                                if (swingLowTime.Date != currentTime.Date)
                                {
                                    Print($"Swing low détecté dans une session différente - VWAP/CWAP non calculé");
                                    currentWaveSwingLow = 0;
                                    currentWaveSwingLowBar = -1;
                                    vwapSequenceActive = false;
                                }
                                else
                                {
                                    vwapAnchorBarIndex = CurrentBar - currentWaveSwingLowBar;
                                    
                                    // Calcul VWAP/CWAP (backfill)
                                    if (ShowVwap || ShowCwap)
                                    {
                                        double runningSumPriceVolume = 0;
                                        double runningSumVolume = 0;
                                        List<Tuple<double, double>> priceVolumeData = new List<Tuple<double, double>>();
                                        
                                        double runningSumPrice = 0;
                                        int barCount = 0;
                                        List<double> priceData = new List<double>();
                                        
                                        for (int i = currentWaveSwingLowBar; i >= 0; i--)
                                        {
                                            if (Time[i].Date != currentTime.Date)
                                            {
                                                break;
                                            }
                                            
                                            double typicalPrice = (High[i] + Low[i] + Close[i]) / 3.0;
                                            double volume = Volume[i];
                                            
                                            runningSumPriceVolume += typicalPrice * volume;
                                            runningSumVolume += volume;
                                            priceVolumeData.Add(new Tuple<double, double>(typicalPrice, volume));
                                            
                                            runningSumPrice += typicalPrice;
                                            barCount++;
                                            priceData.Add(typicalPrice);
                                            
                                            if (ShowVwap && runningSumVolume > 0)
                                            {
                                                double vwap = runningSumPriceVolume / runningSumVolume;
                                                
                                                double sumVolumeSquaredDiff = 0;
                                                foreach (var pv in priceVolumeData)
                                                {
                                                    sumVolumeSquaredDiff += pv.Item2 * Math.Pow(pv.Item1 - vwap, 2);
                                                }
                                                
                                                double variance = sumVolumeSquaredDiff / runningSumVolume;
                                                double stdDev = Math.Sqrt(variance);
                                                
                                                VWAP[i] = vwap;
                                                StdDevP1[i] = vwap + (StdDev1Multiplier * stdDev);
                                                StdDevM1[i] = vwap - (StdDev1Multiplier * stdDev);
                                                StdDevP2[i] = vwap + (StdDev2Multiplier * stdDev);
                                                StdDevM2[i] = vwap - (StdDev2Multiplier * stdDev);
                                            }
                                            
                                            if (ShowCwap && barCount > 0)
                                            {
                                                double cwap = runningSumPrice / barCount;
                                                
                                                double sumSquaredDiff = 0;
                                                foreach (var price in priceData)
                                                {
                                                    sumSquaredDiff += Math.Pow(price - cwap, 2);
                                                }
                                                
                                                double cwapVariance = sumSquaredDiff / barCount;
                                                double cwapStdDev = Math.Sqrt(cwapVariance);
                                                
                                                CWAP[i] = cwap;
                                                CwapStdDevP1[i] = cwap + (StdDev1Multiplier * cwapStdDev);
                                                CwapStdDevM1[i] = cwap - (StdDev1Multiplier * cwapStdDev);
                                                CwapStdDevP2[i] = cwap + (StdDev2Multiplier * cwapStdDev);
                                                CwapStdDevM2[i] = cwap - (StdDev2Multiplier * cwapStdDev);
                                            }
                                        }
                                    }
                                    
                                    waveSize = Math.Round((Close[0] - currentWaveSwingLow) / TickSize);
                                    WaveSwingLow[0] = currentWaveSwingLow;
                                    WaveSizeTicks[0] = waveSize;
                                    
                                    if (ShowSwingLowMarker)
                                    {
                                        Draw.Diamond(this, "WaveSwingLow_" + CurrentBar, false, currentWaveSwingLowBar, currentWaveSwingLow - (2 * TickSize), SwingLowColor);
                                        Draw.Text(this, "SwingLowText_" + CurrentBar, $"SL ({distanceInTicks}t|{downBarsCount}d|{totalBarsCount}b)", currentWaveSwingLowBar, currentWaveSwingLow - (5 * TickSize), SwingLowColor);
                                    }
                                }
                            }
                            
                            if (PaintBars && currentWaveSwingLow > 0)
                            {
                                BarBrush = UpBreakoutColor;
                                CandleOutlineBrush = UpBreakoutColor;
                            }
                        }
                    }
                }
                
                // CALCUL CONTINU DU VWAP ET CWAP
                if (vwapSequenceActive && (ShowVwap || ShowCwap) && vwapAnchorBarIndex != -1 && (CurrentBar > vwapAnchorBarIndex))
                {
                    DateTime anchorTime = Time[CurrentBar - vwapAnchorBarIndex];
                    DateTime currentTime = Time[0];
                    
                    if (anchorTime.Date == currentTime.Date)
                    {
                        int barsToCalculate = CurrentBar - vwapAnchorBarIndex;
                        
                        double sumPriceVolume = 0;
                        double sumVolume = 0;
                        double sumPrice = 0;
                        int barCount = 0;
                        
                        for (int i = 0; i <= barsToCalculate; ++i)
                        {
                            if (Time[i].Date != currentTime.Date)
                                continue;
                            
                            double typicalPrice = (High[i] + Low[i] + Close[i]) / 3.0;
                            double volume = Volume[i];
                            
                            sumPriceVolume += typicalPrice * volume;
                            sumVolume += volume;
                            sumPrice += typicalPrice;
                            barCount++;
                        }
                        
                        if (ShowVwap && sumVolume > 0)
                        {
                            double vwap = sumPriceVolume / sumVolume;
                            VWAP[0] = vwap;
                            
                            double sumVolumeSquaredDiff = 0;
                            for (int i = 0; i <= barsToCalculate; ++i)
                            {
                                if (Time[i].Date != currentTime.Date)
                                    continue;
                                
                                double typicalPrice = (High[i] + Low[i] + Close[i]) / 3.0;
                                double volume = Volume[i];
                                sumVolumeSquaredDiff += volume * Math.Pow(typicalPrice - vwap, 2);
                            }
                            
                            double variance = sumVolumeSquaredDiff / sumVolume;
                            double stdDev = Math.Sqrt(variance);
                            
                            StdDevP1[0] = vwap + (StdDev1Multiplier * stdDev);
                            StdDevM1[0] = vwap - (StdDev1Multiplier * stdDev);
                            StdDevP2[0] = vwap + (StdDev2Multiplier * stdDev);
                            StdDevM2[0] = vwap - (StdDev2Multiplier * stdDev);
                        }
                        
                        if (ShowCwap && barCount > 0)
                        {
                            double cwap = sumPrice / barCount;
                            CWAP[0] = cwap;
                            
                            double sumSquaredDiff = 0;
                            for (int i = 0; i <= barsToCalculate; ++i)
                            {
                                if (Time[i].Date != currentTime.Date)
                                    continue;
                                
                                double typicalPrice = (High[i] + Low[i] + Close[i]) / 3.0;
                                sumSquaredDiff += Math.Pow(typicalPrice - cwap, 2);
                            }
                            
                            double cwapVariance = sumSquaredDiff / barCount;
                            double cwapStdDev = Math.Sqrt(cwapVariance);
                            
                            CwapStdDevP1[0] = cwap + (StdDev1Multiplier * cwapStdDev);
                            CwapStdDevM1[0] = cwap - (StdDev1Multiplier * cwapStdDev);
                            CwapStdDevP2[0] = cwap + (StdDev2Multiplier * cwapStdDev);
                            CwapStdDevM2[0] = cwap - (StdDev2Multiplier * cwapStdDev);
                        }
                    }
                    else
                    {
                        vwapSequenceActive = false;
                        vwapAnchorBarIndex = -1;
                        
                        VWAP[0] = double.NaN;
                        StdDevP1[0] = double.NaN;
                        StdDevM1[0] = double.NaN;
                        StdDevP2[0] = double.NaN;
                        StdDevM2[0] = double.NaN;
                        
                        CWAP[0] = double.NaN;
                        CwapStdDevP1[0] = double.NaN;
                        CwapStdDevM1[0] = double.NaN;
                        CwapStdDevP2[0] = double.NaN;
                        CwapStdDevM2[0] = double.NaN;
                    }
                }
                
				// GESTION DU BREAK-EVEN POUR BARREBREAK
				if (barreBreakPositionOpen && !bbBreakEvenSet && BarreBreakUseBreakEven)
				{
					double targetPrice = barreBreakEntryPrice + (BarreBreakTargetTicks * TickSize);
					
					if (High[0] >= targetPrice)
					{
						bbBreakEvenPrice = barreBreakEntryPrice + (BarreBreakBreakEvenOffset * TickSize);
						SetStopLoss("BB_T2", CalculationMode.Price, bbBreakEvenPrice, false);
						bbBreakEvenSet = true;
						
						Print($"BB Break-Even activé à {Time[0]} - Entry: {barreBreakEntryPrice:F2}, Break-Even: {bbBreakEvenPrice:F2} (+{BarreBreakBreakEvenOffset}t)");
						
						Draw.Line(this, "BBBreakEven_" + CurrentBar, false, 0, bbBreakEvenPrice, -10, bbBreakEvenPrice, 
							Brushes.Gold, DashStyleHelper.Solid, 2);
						Draw.Text(this, "BBBreakEvenText_" + CurrentBar, 
							$"BE+{BarreBreakBreakEvenOffset}t: {bbBreakEvenPrice:F2}", 
							-10, bbBreakEvenPrice, Brushes.Gold);
					}
				}
				
                // DÉTECTION DU SIGNAL FIRSTBUY - AVEC FILTRE HORAIRE INDIVIDUEL
                if (vwapSequenceActive && ShowFirstBuySignal && !firstBuyTriggered && !firstBuyPositionOpen && ShowVwap && IsWithinTradingHours("FB"))
                {
                    if (!double.IsNaN(VWAP[0]) && !double.IsNaN(StdDevP1[0]))
                    {
                        bool isBreakoutBar = (swingHighBroken && currentWaveSwingLowBar == 0);
                        bool previousBarIsUp = CurrentBar > 0 && Close[1] > Open[1];
                        bool lowInZone = Low[0] >= VWAP[0] && Low[0] <= StdDevP1[0];
                        double firstBuyLevel = StdDevP1[0] + (FirstBuyOffsetTicks * TickSize);
                        bool closeAboveLevel = Close[0] > firstBuyLevel;
                        
                        if (!isBreakoutBar && previousBarIsUp && lowInZone && closeAboveLevel)
                        {
							firstBuyTriggered = true;
							fbBreakEvenSet = false;
							fbBreakEvenPrice = 0;
							
							double entryPrice = Close[0];
							double targetPrice = entryPrice + (FirstBuyTargetTicks * TickSize);
							double target2Price = entryPrice + (FirstBuyTarget2Ticks * TickSize);
							double stopLossPrice = entryPrice - (FirstBuyStopLossTicks * TickSize);
							
							fbContractsRemaining = FirstBuyContracts;
							
							// Entrée avec 2 ordres séparés
							EnterLong(FirstBuyTarget1Contracts, "FB_T1");
							EnterLong(FirstBuyContracts - FirstBuyTarget1Contracts, "FB_T2");
							
							firstBuyPositionOpen = true;
							firstBuyEntryPrice = entryPrice;
							
							SetProfitTarget("FB_T1", CalculationMode.Price, targetPrice, false);
							SetProfitTarget("FB_T2", CalculationMode.Price, target2Price, false);
							
							if (FirstBuyUseTrailStop)
							{
								SetTrailStop("FB_T1", CalculationMode.Ticks, FirstBuyTrailStopDistance, false);
								SetTrailStop("FB_T2", CalculationMode.Ticks, FirstBuyTrailStopDistance, false);
							}
							else if (FirstBuyUseParabolicStop)
							{
								SetParabolicStop("FB_T1", CalculationMode.Price, stopLossPrice, false,
									FirstBuyParabolicAcceleration, FirstBuyParabolicAccelerationMax, FirstBuyParabolicAccelerationStep);
								SetParabolicStop("FB_T2", CalculationMode.Price, stopLossPrice, false,
									FirstBuyParabolicAcceleration, FirstBuyParabolicAccelerationMax, FirstBuyParabolicAccelerationStep);
							}
							else
							{
								SetStopLoss("FB_T1", CalculationMode.Price, stopLossPrice, false);
								SetStopLoss("FB_T2", CalculationMode.Price, stopLossPrice, false);
							}
                            
                            Print($"SIGNAL FIRSTBUY EXÉCUTÉ à {Time[0]} - Entry: {entryPrice:F2}, T1: {targetPrice:F2}, T2: {target2Price:F2}, Stop: {stopLossPrice:F2}, Qty: {FirstBuyContracts}");
                            
                            Draw.ArrowUp(this, "FirstBuy_" + CurrentBar, false, 0, Low[0] - (2 * TickSize), FirstBuyMarkerColor);
                            Draw.Text(this, "FirstBuyText_" + CurrentBar, "Buy", 0, Low[0] - (5 * TickSize), FirstBuyTextColor);
                            
                            Draw.Line(this, "FirstBuyTarget1_" + CurrentBar, false, 0, targetPrice, -5, targetPrice, Brushes.Green, DashStyleHelper.Dot, 2);
                            Draw.Text(this, "FirstBuyTarget1Text_" + CurrentBar, $"T1: {targetPrice:F2} (+{FirstBuyTargetTicks}t)", -5, targetPrice, Brushes.Green);
                            
                            Draw.Line(this, "FirstBuyTarget2_" + CurrentBar, false, 0, target2Price, -5, target2Price, Brushes.DarkGreen, DashStyleHelper.Dot, 2);
                            Draw.Text(this, "FirstBuyTarget2Text_" + CurrentBar, $"T2: {target2Price:F2} (+{FirstBuyTarget2Ticks}t)", -5, target2Price, Brushes.DarkGreen);
                            
                            Draw.Line(this, "FirstBuyStopLoss_" + CurrentBar, false, 0, stopLossPrice, -5, stopLossPrice, Brushes.Red, DashStyleHelper.Dot, 2);
                            Draw.Text(this, "FirstBuyStopLossText_" + CurrentBar, $"SL: {stopLossPrice:F2} (-{FirstBuyStopLossTicks}t)", -5, stopLossPrice, Brushes.Red);
                            
                            Draw.Dot(this, "FirstBuyEntry_" + CurrentBar, false, 0, entryPrice, Brushes.White);
                            
                            Draw.Rectangle(this, "FirstBuyZone_" + CurrentBar, false, 5, VWAP[0], 0, StdDevP1[0], 
                                        Brushes.Gold, Brushes.Gold, 20);
                        }
                    }
                }
                
				// GESTION DU BREAK-EVEN POUR FIRSTBUY
				if (firstBuyPositionOpen && !fbBreakEvenSet && FirstBuyUseBreakEven)
				{
					double targetPrice = firstBuyEntryPrice + (FirstBuyTargetTicks * TickSize);
					
					if (High[0] >= targetPrice)
					{
						fbBreakEvenPrice = firstBuyEntryPrice + (FirstBuyBreakEvenOffset * TickSize);
						SetStopLoss("FB_T2", CalculationMode.Price, fbBreakEvenPrice, false);
						fbBreakEvenSet = true;
						
						Print($"FB Break-Even activé à {Time[0]} - Entry: {firstBuyEntryPrice:F2}, Break-Even: {fbBreakEvenPrice:F2} (+{FirstBuyBreakEvenOffset}t)");
						
						Draw.Line(this, "FBBreakEven_" + CurrentBar, false, 0, fbBreakEvenPrice, -10, fbBreakEvenPrice, 
							Brushes.GreenYellow, DashStyleHelper.Solid, 2);
						Draw.Text(this, "FBBreakEvenText_" + CurrentBar, 
							$"BE+{FirstBuyBreakEvenOffset}t: {fbBreakEvenPrice:F2}", 
							-10, fbBreakEvenPrice, Brushes.GreenYellow);
					}
				}
				
                // DÉTECTION DU SIGNAL FIRSTBREAKOUTDOWNUP - AVEC FILTRE HORAIRE INDIVIDUEL
                if (vwapSequenceActive && ShowFirstBreakoutDownUpSignal && !firstBreakoutDownUpTriggered && !fbduPositionOpen && ShowVwap && CurrentBar > 1 && IsWithinTradingHours("FBDU"))
                {
                    if (!double.IsNaN(VWAP[0]) && !double.IsNaN(StdDevP1[0]))
                    {
                        bool previousBarIsDownOrDoji = Close[1] <= Open[1];
                        bool currentBarIsUp = Close[0] > Open[0];
                        bool closeAboveStdDev1 = !RequireCloseAboveStdDev1 || Close[0] >= StdDevP1[0];
                        double vwapWithOffset = VWAP[0] - (FirstBreakoutDownUpZoneOffsetTicks * TickSize);
                        bool lowInTargetZone = Low[0] >= vwapWithOffset && Low[0] <= StdDevP1[0];
                        
                        if (previousBarIsDownOrDoji && currentBarIsUp && lowInTargetZone && closeAboveStdDev1)
                        {
                            firstBreakoutDownUpTriggered = true;
							
							fbduBreakEvenSet = false;
							fbduBreakEvenPrice = 0;
                            
                            double entryPrice = Close[0];
                            double targetPrice = entryPrice + (FirstBreakoutDownUpTargetTicks * TickSize);
                            double target2Price = entryPrice + (FirstBreakoutDownUpTarget2Ticks * TickSize);
                            double stopLossPrice = entryPrice - (FirstBreakoutDownUpStopLossTicks * TickSize);
                            
                            fbduContractsRemaining = FirstBreakoutDownUpContracts;
                            
                            // Entrée avec 2 ordres séparés
							EnterLong(FirstBreakoutDownUpTarget1Contracts, "FBDU_T1");
							EnterLong(FirstBreakoutDownUpContracts - FirstBreakoutDownUpTarget1Contracts, "FBDU_T2");
                            fbduPositionOpen = true;
                            fbduEntryPrice = entryPrice;
                            
                            // Premier target partiel
                            SetProfitTarget("FBDU_T1", CalculationMode.Price, targetPrice, false);
							SetProfitTarget("FBDU_T2", CalculationMode.Price, target2Price, false);
                            
                            // Stop loss initial
                            if (FirstBreakoutDownUpUseTrailStop)
                            {
								SetTrailStop("FBDU_T1", CalculationMode.Ticks, FirstBuyTrailStopDistance, false);
								SetTrailStop("FBDU_T2", CalculationMode.Ticks, FirstBuyTrailStopDistance, false);
                            }
                            else if (FirstBreakoutDownUpUseParabolicStop)
                            {
								SetParabolicStop("FBDU_T1", CalculationMode.Price, stopLossPrice, false,
									FirstBuyParabolicAcceleration, FirstBuyParabolicAccelerationMax, FirstBuyParabolicAccelerationStep);
								SetParabolicStop("FBDU_T2", CalculationMode.Price, stopLossPrice, false,
									FirstBuyParabolicAcceleration, FirstBuyParabolicAccelerationMax, FirstBuyParabolicAccelerationStep);
                            }
                            else
                            {
								SetStopLoss("FBDU_T1", CalculationMode.Price, stopLossPrice, false);
								SetStopLoss("FBDU_T2", CalculationMode.Price, stopLossPrice, false);
                            }
                            
                            Print($"SIGNAL FIRSTBREAKOUTDOWNUP EXÉCUTÉ à {Time[0]} - Entry: {entryPrice:F2}, T1: {targetPrice:F2}, T2: {target2Price:F2}, Stop: {stopLossPrice:F2}, Qty: {FirstBreakoutDownUpContracts}");
                            
                            Draw.ArrowUp(this, "FirstBreakoutDownUp_" + CurrentBar, false, 0, Low[0] - (3 * TickSize), FirstBreakoutDownUpMarkerColor);
                            Draw.Text(this, "FirstBreakoutDownUpText_" + CurrentBar, "FBDU", 0, Low[0] - (6 * TickSize), FirstBreakoutDownUpTextColor);
                            
                            Draw.Line(this, "FBDUTarget1_" + CurrentBar, false, 0, targetPrice, -5, targetPrice, Brushes.LightGreen, DashStyleHelper.Dot, 2);
                            Draw.Text(this, "FBDUTarget1Text_" + CurrentBar, $"T1: {targetPrice:F2} (+{FirstBreakoutDownUpTargetTicks}t)", -5, targetPrice, Brushes.LightGreen);
                            
                            Draw.Line(this, "FBDUTarget2_" + CurrentBar, false, 0, target2Price, -5, target2Price, Brushes.SeaGreen, DashStyleHelper.Dot, 2);
                            Draw.Text(this, "FBDUTarget2Text_" + CurrentBar, $"T2: {target2Price:F2} (+{FirstBreakoutDownUpTarget2Ticks}t)", -5, target2Price, Brushes.SeaGreen);
                            
                            Draw.Line(this, "FBDUStopLoss_" + CurrentBar, false, 0, stopLossPrice, -5, stopLossPrice, Brushes.Pink, DashStyleHelper.Dot, 2);
                            Draw.Text(this, "FBDUStopLossText_" + CurrentBar, $"SL: {stopLossPrice:F2} (-{FirstBreakoutDownUpStopLossTicks}t)", -5, stopLossPrice, Brushes.Pink);
                            
                            Draw.Dot(this, "FBDUEntry_" + CurrentBar, false, 0, entryPrice, Brushes.Aqua);
                            
                            string prevBarType = (Close[1] == Open[1]) ? "=" : "↓";
                            Draw.Text(this, "PreviousBar_" + CurrentBar, prevBarType, 1, High[1] + (2 * TickSize), FirstBreakoutDownUpMarkerColor);
                        }
                    }
                }
				
				// GESTION DU BREAK-EVEN POUR FBDU (existant)
				if (fbduPositionOpen && !fbduBreakEvenSet && FirstBreakoutDownUpUseBreakEven)
				{
					double targetPrice = fbduEntryPrice + (FirstBreakoutDownUpTargetTicks * TickSize);
					
					if (High[0] >= targetPrice)
					{
						fbduBreakEvenPrice = fbduEntryPrice + (FirstBreakoutDownUpBreakEvenOffset * TickSize);
						SetStopLoss("FBDU_T2", CalculationMode.Price, fbduBreakEvenPrice, false);
						fbduBreakEvenSet = true;
						
						Print($"FBDU Break-Even activé à {Time[0]} - Entry: {fbduEntryPrice:F2}, Break-Even: {fbduBreakEvenPrice:F2} (+{FirstBreakoutDownUpBreakEvenOffset}t)");
						
						Draw.Line(this, "FBDUBreakEven_" + CurrentBar, false, 0, fbduBreakEvenPrice, -10, fbduBreakEvenPrice, 
							Brushes.DodgerBlue, DashStyleHelper.Solid, 2);
						Draw.Text(this, "FBDUBreakEvenText_" + CurrentBar, 
							$"BE+{FirstBreakoutDownUpBreakEvenOffset}t: {fbduBreakEvenPrice:F2}", 
							-10, fbduBreakEvenPrice, Brushes.DodgerBlue);
					}
				}
                
                // DÉTECTION DU SIGNAL FIRSTBREAKOUTDOWNUPSTD2 - AVEC FILTRE HORAIRE INDIVIDUEL
                if (vwapSequenceActive && ShowFirstBreakoutDownUpStd2Signal && !firstBreakoutDownUpStd2Triggered && !fbdu2PositionOpen && ShowVwap && CurrentBar > 1 && IsWithinTradingHours("FBDU2"))
                {
                    if (!double.IsNaN(StdDevP1[0]) && !double.IsNaN(StdDevP2[0]))
                    {
                        bool previousBarIsDownOrDoji = Close[1] <= Open[1];
                        bool currentBarIsUp = Close[0] > Open[0];
                        bool closeAboveStdDev2 = !RequireCloseAboveStdDev2 || Close[0] >= StdDevP2[0];
                        bool lowInTargetZone = Low[0] >= StdDevP1[0] && Low[0] <= StdDevP2[0];
                        
                        if (previousBarIsDownOrDoji && currentBarIsUp && lowInTargetZone && closeAboveStdDev2)
                        {
                            firstBreakoutDownUpStd2Triggered = true;
                            fbdu2BreakEvenSet = false;
                            fbdu2BreakEvenPrice = 0;
							
                            double entryPrice = Close[0];
                            double targetPrice = entryPrice + (FirstBreakoutDownUpStd2TargetTicks * TickSize);
                            double target2Price = entryPrice + (FirstBreakoutDownUpStd2Target2Ticks * TickSize);
                            double stopLossPrice = entryPrice - (FirstBreakoutDownUpStd2StopLossTicks * TickSize);
                            
                            fbdu2ContractsRemaining = FirstBreakoutDownUpStd2Contracts;
                            
                            // Entrée avec 2 ordres séparés
							EnterLong(FirstBreakoutDownUpStd2Target1Contracts, "FBDU2_T1");
							EnterLong(FirstBreakoutDownUpStd2Contracts - FirstBreakoutDownUpStd2Target1Contracts, "FBDU2_T2");
                            fbdu2PositionOpen = true;
                            fbdu2EntryPrice = entryPrice;
                            
                            // Premier target partiel
                            SetProfitTarget("FBDU2_T1", CalculationMode.Price, targetPrice, false);
							SetProfitTarget("FBDU2_T2", CalculationMode.Price, target2Price, false);
                            
                            // Stop loss initial
                            if (FirstBreakoutDownUpStd2UseTrailStop)
                            {
								SetTrailStop("FBDU2_T1", CalculationMode.Ticks, FirstBuyTrailStopDistance, false);
								SetTrailStop("FBDU2_T2", CalculationMode.Ticks, FirstBuyTrailStopDistance, false);
                            }
                            else if (FirstBreakoutDownUpStd2UseParabolicStop)
                            {
								SetParabolicStop("FBDU2_T1", CalculationMode.Price, stopLossPrice, false,
									FirstBuyParabolicAcceleration, FirstBuyParabolicAccelerationMax, FirstBuyParabolicAccelerationStep);
								SetParabolicStop("FBDU2_T2", CalculationMode.Price, stopLossPrice, false,
									FirstBuyParabolicAcceleration, FirstBuyParabolicAccelerationMax, FirstBuyParabolicAccelerationStep);
                            }
                            else
                            {
                                SetStopLoss("FBDU2_T1", CalculationMode.Price, stopLossPrice, false);
                                SetStopLoss("FBDU2_T2", CalculationMode.Price, stopLossPrice, false);
                            }
                            
                            Print($"SIGNAL FIRSTBREAKOUTDOWNUPSTD2 EXÉCUTÉ à {Time[0]} - Entry: {entryPrice:F2}, T1: {targetPrice:F2}, T2: {target2Price:F2}, Stop: {stopLossPrice:F2}, Qty: {FirstBreakoutDownUpStd2Contracts}");
                            
                            Draw.ArrowUp(this, "FirstBreakoutDownUpStd2_" + CurrentBar, false, 0, Low[0] - (3 * TickSize), FirstBreakoutDownUpStd2MarkerColor);
                            Draw.Text(this, "FirstBreakoutDownUpStd2Text_" + CurrentBar, "FBDU2", 0, Low[0] - (6 * TickSize), FirstBreakoutDownUpStd2TextColor);
                            
                            Draw.Line(this, "FBDU2Target1_" + CurrentBar, false, 0, targetPrice, -5, targetPrice, Brushes.Plum, DashStyleHelper.Dot, 2);
                            Draw.Text(this, "FBDU2Target1Text_" + CurrentBar, $"T1: {targetPrice:F2} (+{FirstBreakoutDownUpStd2TargetTicks}t)", -5, targetPrice, Brushes.Plum);
                            
                            Draw.Line(this, "FBDU2Target2_" + CurrentBar, false, 0, target2Price, -5, target2Price, Brushes.MediumPurple, DashStyleHelper.Dot, 2);
                            Draw.Text(this, "FBDU2Target2Text_" + CurrentBar, $"T2: {target2Price:F2} (+{FirstBreakoutDownUpStd2Target2Ticks}t)", -5, target2Price, Brushes.MediumPurple);
                            
                            Draw.Line(this, "FBDU2StopLoss_" + CurrentBar, false, 0, stopLossPrice, -5, stopLossPrice, Brushes.Violet, DashStyleHelper.Dot, 2);
                            Draw.Text(this, "FBDU2StopLossText_" + CurrentBar, $"SL: {stopLossPrice:F2} (-{FirstBreakoutDownUpStd2StopLossTicks}t)", -5, stopLossPrice, Brushes.Violet);
                            
                            Draw.Dot(this, "FBDU2Entry_" + CurrentBar, false, 0, entryPrice, Brushes.Purple);
                            
                            string prevBarType = (Close[1] == Open[1]) ? "=" : "↓";
                            Draw.Text(this, "PreviousBarStd2_" + CurrentBar, prevBarType, 1, High[1] + (4 * TickSize), FirstBreakoutDownUpStd2MarkerColor);
                            
                            Draw.Rectangle(this, "FBDU2Zone_" + CurrentBar, false, 5, StdDevP1[0], 0, StdDevP2[0], 
                                        Brushes.Purple, Brushes.Purple, 15);
                        }
                    }
                }
                
				// GESTION DU BREAK-EVEN POUR FBDU2
				if (fbdu2PositionOpen && !fbdu2BreakEvenSet && FirstBreakoutDownUpStd2UseBreakEven)
				{
					double targetPrice = fbdu2EntryPrice + (FirstBreakoutDownUpStd2TargetTicks * TickSize);
					
					if (High[0] >= targetPrice)
					{
						fbdu2BreakEvenPrice = fbdu2EntryPrice + (FirstBreakoutDownUpStd2BreakEvenOffset * TickSize);
						SetStopLoss("FBDU2_T2", CalculationMode.Price, fbdu2BreakEvenPrice, false);
						fbdu2BreakEvenSet = true;
						
						Print($"FBDU2 Break-Even activé à {Time[0]} - Entry: {fbdu2EntryPrice:F2}, Break-Even: {fbdu2BreakEvenPrice:F2} (+{FirstBreakoutDownUpStd2BreakEvenOffset}t)");
						
						Draw.Line(this, "FBDU2BreakEven_" + CurrentBar, false, 0, fbdu2BreakEvenPrice, -10, fbdu2BreakEvenPrice, 
							Brushes.MediumPurple, DashStyleHelper.Solid, 2);
						Draw.Text(this, "FBDU2BreakEvenText_" + CurrentBar, 
							$"BE+{FirstBreakoutDownUpStd2BreakEvenOffset}t: {fbdu2BreakEvenPrice:F2}", 
							-10, fbdu2BreakEvenPrice, Brushes.MediumPurple);
					}
				}
				
            }
            catch (Exception ex)
            {
                Print("Erreur dans aaaSBVCfbS600: " + ex.Message);
            }
        }
        
        // Méthode pour fermer toutes les positions ouvertes
        private void CloseAllPositions(string reason)
        {
            Print($"Fermeture de toutes les positions - Raison: {reason}");
            
            if (barreBreakPositionOpen)
			{
				ExitLong("BB_T1");
				ExitLong("BB_T2");
				barreBreakPositionOpen = false;
				bbContractsRemaining = 0;
			}
			
			if (firstBuyPositionOpen)
			{
				ExitLong("FB_T1");
				ExitLong("FB_T2");
				firstBuyPositionOpen = false;
				fbContractsRemaining = 0;
			}
			
			if (fbduPositionOpen)
			{
				ExitLong("FBDU_T1");
				ExitLong("FBDU_T2");
				fbduPositionOpen = false;
				fbduContractsRemaining = 0;
			}
			
			if (fbdu2PositionOpen)
			{
				ExitLong("FBDU2_T1");
				ExitLong("FBDU2_T2");
				fbdu2PositionOpen = false;
				fbdu2ContractsRemaining = 0;
			}
        }
        
        // Méthode pour réinitialiser la séquence VWAP
        private void EndVwapSequence()
        {
            vwapSequenceActive = false;
            vwapAnchorBarIndex = -1;
            consecutiveBarsInValueArea = 0;
            firstBuyTriggered = false;
            firstBreakoutDownUpTriggered = false;
            firstBreakoutDownUpStd2Triggered = false;
            
			// Réinitialiser tous les break-evens
			bbBreakEvenSet = false;
			bbBreakEvenPrice = 0;
			fbBreakEvenSet = false;
			fbBreakEvenPrice = 0;
			fbduBreakEvenSet = false;
			fbduBreakEvenPrice = 0;
			fbdu2BreakEvenSet = false;
			fbdu2BreakEvenPrice = 0;
			
            lastSwingHigh = 0;
            lastSwingHighBar = -1;
            swingHighBroken = false;
            currentWaveSwingLow = 0;
            currentWaveSwingLowBar = -1;
            waveSize = 0;
            
            VWAP[0] = double.NaN;
            StdDevP1[0] = double.NaN;
            StdDevM1[0] = double.NaN;
            StdDevP2[0] = double.NaN;
            StdDevM2[0] = double.NaN;
            
            CWAP[0] = double.NaN;
            CwapStdDevP1[0] = double.NaN;
            CwapStdDevM1[0] = double.NaN;
            CwapStdDevP2[0] = double.NaN;
            CwapStdDevM2[0] = double.NaN;
        }
        
        // Gérer les états des positions
        protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
        {
            if (position.MarketPosition == MarketPosition.Flat)
            {
                if (position.Instrument == Instrument)
                {
                    string signalName = position.ToString();
                    
                    if (signalName.Contains("BB"))
                    {
                        barreBreakPositionOpen = false;
                        bbContractsRemaining = 0;
						bbBreakEvenSet = false;
						bbBreakEvenPrice = 0;
                    }
                    else if (signalName.Contains("FB") && !signalName.Contains("FBDU"))
                    {
                        firstBuyPositionOpen = false;
                        fbContractsRemaining = 0;
						fbBreakEvenSet = false;
						fbBreakEvenPrice = 0;
                    }
                    else if (signalName.Contains("FBDU2"))
                    {
                        fbdu2PositionOpen = false;
                        fbdu2ContractsRemaining = 0;
						fbdu2BreakEvenSet = false;
						fbdu2BreakEvenPrice = 0;
                    }
					else if (signalName.Contains("FBDU") && !signalName.Contains("FBDU2"))
					{
						fbduPositionOpen = false;
						fbduContractsRemaining = 0;
						fbduBreakEvenSet = false;
						fbduBreakEvenPrice = 0;
					}
                }
            }
        }
        
        #region Properties - TOUS LES PARAMÈTRES
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Strength", Description = "Nombre de barres pour identifier un swing", Order = 1, GroupName = "Paramètres")]
        public int Strength
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Look Back Period", Description = "Période de recherche pour trouver les swings", Order = 2, GroupName = "Paramètres")]
        public int LookBackPeriod
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Offset (Ticks)", Description = "Offset en ticks au-dessus du swing high", Order = 3, GroupName = "Paramètres")]
        public int OffsetTicks
        { get; set; }
        
        // Paramètres de validation de cassure
        [NinjaScriptProperty]
        [Display(Name = "Activer Distance Minimale", Description = "Active la validation par distance minimale swing low-high", Order = 4, GroupName = "Validation Cassure")]
        public bool EnableMinimumDistance
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Distance Minimale (Ticks)", Description = "Distance minimale requise entre swing low et swing high", Order = 5, GroupName = "Validation Cassure")]
        public int MinimumSwingDistanceTicks
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Qualité Montée", Description = "Active le contrôle du nombre de barres down dans la montée", Order = 6, GroupName = "Validation Cassure")]
        public bool EnableUpBarFilter
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Max Barres Down Autorisées", Description = "Nombre maximum de barres down tolérées", Order = 7, GroupName = "Validation Cassure")]
        public int MaxDownBarsAllowed
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Nombre de Barres", Description = "Active le contrôle du nombre total de barres dans la montée", Order = 8, GroupName = "Validation Cassure")]
        public bool EnableBarCountFilter
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Minimum Barres Requises", Description = "Nombre minimum de barres entre swing low et cassure", Order = 9, GroupName = "Validation Cassure")]
        public int MinBarsRequired
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Maximum Barres Autorisées", Description = "Nombre maximum de barres entre swing low et cassure", Order = 10, GroupName = "Validation Cassure")]
        public int MaxBarsAllowed
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Cassures Rejetées", Description = "Marquer visuellement les cassures rejetées", Order = 11, GroupName = "Validation Cassure")]
        public bool ShowRejectedBreakouts
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Barre Cassure", Description = "Active le contrôle de la qualité de la barre de cassure", Order = 12, GroupName = "Validation Cassure")]
        public bool EnableBreakoutBarFilter
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Taille Min Barre Cassure (Ticks)", Description = "Taille minimale de la barre de cassure en ticks", Order = 13, GroupName = "Validation Cassure")]
        public int MinBreakoutBarSizeTicks
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Close > High Précédent", Description = "Exiger que le close de la barre de cassure soit > au high de la barre précédente", Order = 14, GroupName = "Validation Cassure")]
        public bool RequireCloseAbovePreHigh
        { get; set; }
        
        // Paramètres d'affichage
        [NinjaScriptProperty]
        [Display(Name = "Colorer les Barres", Description = "Colorer les barres de breakout", Order = 4, GroupName = "Affichage")]
        public bool PaintBars
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Ligne Swing High", Description = "Afficher la ligne du swing high", Order = 5, GroupName = "Affichage")]
        public bool ShowSwingHighLine
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Swing Low", Description = "Marquer le swing low de la vague", Order = 6, GroupName = "Affichage")]
        public bool ShowSwingLowMarker
        { get; set; }
        
        // Propriétés VWAP
        [NinjaScriptProperty]
        [Display(Name = "Afficher VWAP Ancré", Description = "Active le VWAP ancré au swing low", Order = 1, GroupName = "VWAP Ancré")]
        public bool ShowVwap { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "StdDev Multiplicateur 1", Description = "Multiplicateur pour la 1ère bande d'écart-type", Order = 2, GroupName = "VWAP Ancré")]
        public double StdDev1Multiplier { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "StdDev Multiplicateur 2", Description = "Multiplicateur pour la 2ème bande d'écart-type", Order = 3, GroupName = "VWAP Ancré")]
        public double StdDev2Multiplier { get; set; }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur VWAP", Description = "Couleur de la ligne VWAP", Order = 4, GroupName = "VWAP Ancré")]
        public Brush VwapColor { get; set; }
        
        [Browsable(false)]
        public string VwapColorSerializable
        {
            get { return Serialize.BrushToString(VwapColor); }
            set { VwapColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Bande StdDev 1", Description = "Couleur pour les bandes +/- 1 StdDev", Order = 5, GroupName = "VWAP Ancré")]
        public Brush StdDev1BandColor { get; set; }
        
        [Browsable(false)]
        public string StdDev1BandColorSerializable
        {
            get { return Serialize.BrushToString(StdDev1BandColor); }
            set { StdDev1BandColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Bande StdDev 2", Description = "Couleur pour les bandes +/- 2 StdDev", Order = 6, GroupName = "VWAP Ancré")]
        public Brush StdDev2BandColor { get; set; }
        
        [Browsable(false)]
        public string StdDev2BandColorSerializable
        {
            get { return Serialize.BrushToString(StdDev2BandColor); }
            set { StdDev2BandColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Largeur Ligne VWAP", Description = "Largeur de la ligne VWAP", Order = 7, GroupName = "VWAP Ancré")]
        public int VwapLineWidth { get; set; }
        
        // Propriétés CWAP
        [NinjaScriptProperty]
        [Display(Name = "Afficher CWAP Ancré", Description = "Active le CWAP ancré au swing low", Order = 1, GroupName = "CWAP Ancré")]
        public bool ShowCwap { get; set; }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur CWAP", Description = "Couleur de la ligne CWAP", Order = 2, GroupName = "CWAP Ancré")]
        public Brush CwapColor { get; set; }
        
        [Browsable(false)]
        public string CwapColorSerializable
        {
            get { return Serialize.BrushToString(CwapColor); }
            set { CwapColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Bande CWAP StdDev 1", Description = "Couleur pour les bandes +/- 1 StdDev du CWAP", Order = 3, GroupName = "CWAP Ancré")]
        public Brush CwapStdDev1BandColor { get; set; }
        
        [Browsable(false)]
        public string CwapStdDev1BandColorSerializable
        {
            get { return Serialize.BrushToString(CwapStdDev1BandColor); }
            set { CwapStdDev1BandColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Bande CWAP StdDev 2", Description = "Couleur pour les bandes +/- 2 StdDev du CWAP", Order = 4, GroupName = "CWAP Ancré")]
        public Brush CwapStdDev2BandColor { get; set; }
        
        [Browsable(false)]
        public string CwapStdDev2BandColorSerializable
        {
            get { return Serialize.BrushToString(CwapStdDev2BandColor); }
            set { CwapStdDev2BandColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Largeur Ligne CWAP", Description = "Largeur de la ligne CWAP", Order = 5, GroupName = "CWAP Ancré")]
        public int CwapLineWidth { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Offset Cassure VWAP (Ticks)", Description = "Offset en ticks sous le VWAP pour détecter la cassure", Order = 8, GroupName = "VWAP Ancré")]
        public int OffsetTicksVwap
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Marqueur Cassure VWAP", Description = "Afficher un marqueur au point de cassure du VWAP", Order = 9, GroupName = "VWAP Ancré")]
        public bool ShowVwapBreakMarker
        { get; set; }
        
        [XmlIgnore]
        [Display(Name = "Couleur Cassure VWAP", Description = "Couleur du marqueur de cassure VWAP", Order = 10, GroupName = "VWAP Ancré")]
        public Brush VwapBreakColor
        { get; set; }
        
        [Browsable(false)]
        public string VwapBreakColorSerializable
        {
            get { return Serialize.BrushToString(VwapBreakColor); }
            set { VwapBreakColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Activer Sortie Value Area", Description = "Active la sortie si consolidation dans la Value Area", Order = 11, GroupName = "VWAP Ancré")]
        public bool EnableValueAreaExit
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Barres dans Value Area pour Sortie", Description = "Nombre de barres consécutives dans la VA pour terminer la séquence", Order = 12, GroupName = "VWAP Ancré")]
        public int BarsInValueAreaForExit
        { get; set; }
        
        // Paramètres de gestion des sorties
        [NinjaScriptProperty]
        [Display(Name = "Fermer Positions sur Cassure VWAP", Description = "Ferme toutes les positions si le VWAP est cassé", Order = 1, GroupName = "Gestion Sorties")]
        public bool ClosePositionsOnVwapBreak { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Fermer Positions sur Consolidation VA", Description = "Ferme toutes les positions en cas de consolidation dans Value Area", Order = 2, GroupName = "Gestion Sorties")]
        public bool ClosePositionsOnVAConsolidation { get; set; }
        
        // Propriétés Signal BarreBreak avec DOUBLE FILTRE HORAIRE
        [NinjaScriptProperty]
        [Display(Name = "Afficher Signal BarreBreak", Description = "Active le signal BarreBreak (BB) sur la barre de cassure", Order = 1, GroupName = "Signal BarreBreak")]
        public bool ShowBarreBreakSignal { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 1 BarreBreak (Ticks)", Description = "Nombre de ticks pour le premier target", Order = 2, GroupName = "Signal BarreBreak")]
        public int BarreBreakTargetTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 2 BarreBreak (Ticks)", Description = "Nombre de ticks pour le deuxième target", Order = 3, GroupName = "Signal BarreBreak")]
        public int BarreBreakTarget2Ticks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss BarreBreak (Ticks)", Description = "Nombre de ticks pour le stop loss", Order = 4, GroupName = "Signal BarreBreak")]
        public int BarreBreakStopLossTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Nombre de Contrats BB", Description = "Nombre total de contrats pour BarreBreak", Order = 5, GroupName = "Signal BarreBreak")]
        public int BarreBreakContracts { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Contrats Target 1 BB", Description = "Nombre de contrats à sortir au premier target", Order = 6, GroupName = "Signal BarreBreak")]
        public int BarreBreakTarget1Contracts { get; set; }
        
        // Premier filtre horaire BB
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Horaire 1 BB", Description = "Active le premier filtre horaire pour BarreBreak", Order = 7, GroupName = "Signal BarreBreak")]
        public bool BarreBreakEnableTimeFilter { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Début 1 BB", Description = "Heure de début pour la première plage BarreBreak", Order = 8, GroupName = "Signal BarreBreak")]
        public DateTime BarreBreakStartTime { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Fin 1 BB", Description = "Heure de fin pour la première plage BarreBreak", Order = 9, GroupName = "Signal BarreBreak")]
        public DateTime BarreBreakEndTime { get; set; }
        
        // Deuxième filtre horaire BB
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Horaire 2 BB", Description = "Active le deuxième filtre horaire pour BarreBreak", Order = 10, GroupName = "Signal BarreBreak")]
        public bool BarreBreakEnableTimeFilter2 { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Début 2 BB", Description = "Heure de début pour la deuxième plage BarreBreak", Order = 11, GroupName = "Signal BarreBreak")]
        public DateTime BarreBreakStartTime2 { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Fin 2 BB", Description = "Heure de fin pour la deuxième plage BarreBreak", Order = 12, GroupName = "Signal BarreBreak")]
        public DateTime BarreBreakEndTime2 { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Trail Stop BB", Description = "Active le trail stop pour BarreBreak", Order = 13, GroupName = "Signal BarreBreak")]
        public bool BarreBreakUseTrailStop { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trail Stop Distance BB (Ticks)", Description = "Distance du trail stop en ticks", Order = 14, GroupName = "Signal BarreBreak")]
        public int BarreBreakTrailStopDistance { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Parabolic Stop BB", Description = "Active le parabolic stop pour BarreBreak", Order = 15, GroupName = "Signal BarreBreak")]
        public bool BarreBreakUseParabolicStop { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration BB", Description = "Facteur d'accélération initial", Order = 16, GroupName = "Signal BarreBreak")]
        public double BarreBreakParabolicAcceleration { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration Max BB", Description = "Facteur d'accélération maximum", Order = 17, GroupName = "Signal BarreBreak")]
        public double BarreBreakParabolicAccelerationMax { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration Step BB", Description = "Incrément d'accélération", Order = 18, GroupName = "Signal BarreBreak")]
        public double BarreBreakParabolicAccelerationStep { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Break-Even BB", Description = "Active le break-even après le premier target", Order = 19, GroupName = "Signal BarreBreak")]
        public bool BarreBreakUseBreakEven { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Break-Even Offset BB (Ticks)", Description = "Offset en ticks au-dessus du prix d'entrée pour le break-even", Order = 20, GroupName = "Signal BarreBreak")]
        public int BarreBreakBreakEvenOffset { get; set; }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Marqueur BarreBreak", Description = "Couleur de la flèche du signal BarreBreak", Order = 21, GroupName = "Signal BarreBreak")]
        public Brush BarreBreakMarkerColor { get; set; }
        
        [Browsable(false)]
        public string BarreBreakMarkerColorSerializable
        {
            get { return Serialize.BrushToString(BarreBreakMarkerColor); }
            set { BarreBreakMarkerColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Texte BarreBreak", Description = "Couleur du texte du signal BarreBreak", Order = 22, GroupName = "Signal BarreBreak")]
        public Brush BarreBreakTextColor { get; set; }
        
        [Browsable(false)]
        public string BarreBreakTextColorSerializable
        {
            get { return Serialize.BrushToString(BarreBreakTextColor); }
            set { BarreBreakTextColor = Serialize.StringToBrush(value); }
        }
        
        // Propriétés Signal FirstBuy avec DOUBLE FILTRE HORAIRE
        [NinjaScriptProperty]
        [Display(Name = "Afficher Signal FirstBuy", Description = "Active le signal d'achat firstBuy", Order = 1, GroupName = "Signal FirstBuy")]
        public bool ShowFirstBuySignal { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Offset FirstBuy (Ticks)", Description = "Offset en ticks au-dessus de StdDev+1 pour confirmer le signal", Order = 2, GroupName = "Signal FirstBuy")]
        public int FirstBuyOffsetTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 1 FirstBuy (Ticks)", Description = "Nombre de ticks pour le premier target", Order = 3, GroupName = "Signal FirstBuy")]
        public int FirstBuyTargetTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 2 FirstBuy (Ticks)", Description = "Nombre de ticks pour le deuxième target", Order = 4, GroupName = "Signal FirstBuy")]
        public int FirstBuyTarget2Ticks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss FirstBuy (Ticks)", Description = "Nombre de ticks pour le stop loss", Order = 5, GroupName = "Signal FirstBuy")]
        public int FirstBuyStopLossTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Nombre de Contrats FB", Description = "Nombre total de contrats pour FirstBuy", Order = 6, GroupName = "Signal FirstBuy")]
        public int FirstBuyContracts { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Contrats Target 1 FB", Description = "Nombre de contrats à sortir au premier target", Order = 7, GroupName = "Signal FirstBuy")]
        public int FirstBuyTarget1Contracts { get; set; }
        
        // Premier filtre horaire FB
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Horaire 1 FB", Description = "Active le premier filtre horaire pour FirstBuy", Order = 8, GroupName = "Signal FirstBuy")]
        public bool FirstBuyEnableTimeFilter { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Début 1 FB", Description = "Heure de début pour la première plage FirstBuy", Order = 9, GroupName = "Signal FirstBuy")]
        public DateTime FirstBuyStartTime { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Fin 1 FB", Description = "Heure de fin pour la première plage FirstBuy", Order = 10, GroupName = "Signal FirstBuy")]
        public DateTime FirstBuyEndTime { get; set; }
        
        // Deuxième filtre horaire FB
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Horaire 2 FB", Description = "Active le deuxième filtre horaire pour FirstBuy", Order = 11, GroupName = "Signal FirstBuy")]
        public bool FirstBuyEnableTimeFilter2 { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Début 2 FB", Description = "Heure de début pour la deuxième plage FirstBuy", Order = 12, GroupName = "Signal FirstBuy")]
        public DateTime FirstBuyStartTime2 { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Fin 2 FB", Description = "Heure de fin pour la deuxième plage FirstBuy", Order = 13, GroupName = "Signal FirstBuy")]
        public DateTime FirstBuyEndTime2 { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Trail Stop FB", Description = "Active le trail stop pour FirstBuy", Order = 14, GroupName = "Signal FirstBuy")]
        public bool FirstBuyUseTrailStop { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trail Stop Distance FB (Ticks)", Description = "Distance du trail stop en ticks", Order = 15, GroupName = "Signal FirstBuy")]
        public int FirstBuyTrailStopDistance { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Parabolic Stop FB", Description = "Active le parabolic stop pour FirstBuy", Order = 16, GroupName = "Signal FirstBuy")]
        public bool FirstBuyUseParabolicStop { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration FB", Description = "Facteur d'accélération initial", Order = 17, GroupName = "Signal FirstBuy")]
        public double FirstBuyParabolicAcceleration { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration Max FB", Description = "Facteur d'accélération maximum", Order = 18, GroupName = "Signal FirstBuy")]
        public double FirstBuyParabolicAccelerationMax { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration Step FB", Description = "Incrément d'accélération", Order = 19, GroupName = "Signal FirstBuy")]
        public double FirstBuyParabolicAccelerationStep { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Break-Even FB", Description = "Active le break-even après le premier target", Order = 20, GroupName = "Signal FirstBuy")]
        public bool FirstBuyUseBreakEven { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Break-Even Offset FB (Ticks)", Description = "Offset en ticks au-dessus du prix d'entrée pour le break-even", Order = 21, GroupName = "Signal FirstBuy")]
        public int FirstBuyBreakEvenOffset { get; set; }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Marqueur FirstBuy", Description = "Couleur de la flèche du signal firstBuy", Order = 22, GroupName = "Signal FirstBuy")]
        public Brush FirstBuyMarkerColor { get; set; }
        
        [Browsable(false)]
        public string FirstBuyMarkerColorSerializable
        {
            get { return Serialize.BrushToString(FirstBuyMarkerColor); }
            set { FirstBuyMarkerColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Texte FirstBuy", Description = "Couleur du texte du signal firstBuy", Order = 23, GroupName = "Signal FirstBuy")]
        public Brush FirstBuyTextColor { get; set; }
        
        [Browsable(false)]
        public string FirstBuyTextColorSerializable
        {
            get { return Serialize.BrushToString(FirstBuyTextColor); }
            set { FirstBuyTextColor = Serialize.StringToBrush(value); }
        }
        
        // Propriétés Signal FirstBreakoutDownUp avec DOUBLE FILTRE HORAIRE
        [NinjaScriptProperty]
        [Display(Name = "Afficher Signal FirstBreakoutDownUp", Description = "Active le signal firstbreakoutdownup", Order = 1, GroupName = "Signal FirstBreakoutDownUp")]
        public bool ShowFirstBreakoutDownUpSignal { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Exiger Close > StdDev+1", Description = "Exige que la clôture soit au-dessus de StdDev+1", Order = 2, GroupName = "Signal FirstBreakoutDownUp")]
        public bool RequireCloseAboveStdDev1 { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 1 FBDU (Ticks)", Description = "Nombre de ticks pour le premier target", Order = 3, GroupName = "Signal FirstBreakoutDownUp")]
        public int FirstBreakoutDownUpTargetTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 2 FBDU (Ticks)", Description = "Nombre de ticks pour le deuxième target", Order = 4, GroupName = "Signal FirstBreakoutDownUp")]
        public int FirstBreakoutDownUpTarget2Ticks { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Offset Zone FBDU (Ticks)", Description = "Extension de la zone vers le bas en ticks", Order = 5, GroupName = "Signal FirstBreakoutDownUp")]
        public int FirstBreakoutDownUpZoneOffsetTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss FBDU (Ticks)", Description = "Nombre de ticks pour le stop loss", Order = 6, GroupName = "Signal FirstBreakoutDownUp")]
        public int FirstBreakoutDownUpStopLossTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Nombre de Contrats FBDU", Description = "Nombre total de contrats pour FBDU", Order = 7, GroupName = "Signal FirstBreakoutDownUp")]
        public int FirstBreakoutDownUpContracts { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Contrats Target 1 FBDU", Description = "Nombre de contrats à sortir au premier target", Order = 8, GroupName = "Signal FirstBreakoutDownUp")]
        public int FirstBreakoutDownUpTarget1Contracts { get; set; }
        
        // Premier filtre horaire FBDU
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Horaire 1 FBDU", Description = "Active le premier filtre horaire pour FBDU", Order = 9, GroupName = "Signal FirstBreakoutDownUp")]
        public bool FirstBreakoutDownUpEnableTimeFilter { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Début 1 FBDU", Description = "Heure de début pour la première plage FBDU", Order = 10, GroupName = "Signal FirstBreakoutDownUp")]
        public DateTime FirstBreakoutDownUpStartTime { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Fin 1 FBDU", Description = "Heure de fin pour la première plage FBDU", Order = 11, GroupName = "Signal FirstBreakoutDownUp")]
        public DateTime FirstBreakoutDownUpEndTime { get; set; }
        
        // Deuxième filtre horaire FBDU
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Horaire 2 FBDU", Description = "Active le deuxième filtre horaire pour FBDU", Order = 12, GroupName = "Signal FirstBreakoutDownUp")]
        public bool FirstBreakoutDownUpEnableTimeFilter2 { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Début 2 FBDU", Description = "Heure de début pour la deuxième plage FBDU", Order = 13, GroupName = "Signal FirstBreakoutDownUp")]
        public DateTime FirstBreakoutDownUpStartTime2 { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Fin 2 FBDU", Description = "Heure de fin pour la deuxième plage FBDU", Order = 14, GroupName = "Signal FirstBreakoutDownUp")]
        public DateTime FirstBreakoutDownUpEndTime2 { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Trail Stop FBDU", Description = "Active le trail stop pour FBDU", Order = 15, GroupName = "Signal FirstBreakoutDownUp")]
        public bool FirstBreakoutDownUpUseTrailStop { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trail Stop Distance FBDU (Ticks)", Description = "Distance du trail stop en ticks", Order = 16, GroupName = "Signal FirstBreakoutDownUp")]
        public int FirstBreakoutDownUpTrailStopDistance { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Parabolic Stop FBDU", Description = "Active le parabolic stop pour FBDU", Order = 17, GroupName = "Signal FirstBreakoutDownUp")]
        public bool FirstBreakoutDownUpUseParabolicStop { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration FBDU", Description = "Facteur d'accélération initial", Order = 18, GroupName = "Signal FirstBreakoutDownUp")]
        public double FirstBreakoutDownUpParabolicAcceleration { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration Max FBDU", Description = "Facteur d'accélération maximum", Order = 19, GroupName = "Signal FirstBreakoutDownUp")]
        public double FirstBreakoutDownUpParabolicAccelerationMax { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration Step FBDU", Description = "Incrément d'accélération", Order = 20, GroupName = "Signal FirstBreakoutDownUp")]
        public double FirstBreakoutDownUpParabolicAccelerationStep { get; set; }
        
		[NinjaScriptProperty]
		[Display(Name = "Utiliser Break-Even FBDU", Description = "Active le break-even après le premier target", Order = 21, GroupName = "Signal FirstBreakoutDownUp")]
		public bool FirstBreakoutDownUpUseBreakEven { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Break-Even Offset FBDU (Ticks)", Description = "Offset en ticks au-dessus du prix d'entrée pour le break-even", Order = 22, GroupName = "Signal FirstBreakoutDownUp")]
		public int FirstBreakoutDownUpBreakEvenOffset { get; set; }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Marqueur FBDU", Description = "Couleur de la flèche du signal", Order = 23, GroupName = "Signal FirstBreakoutDownUp")]
        public Brush FirstBreakoutDownUpMarkerColor { get; set; }
        
        [Browsable(false)]
        public string FirstBreakoutDownUpMarkerColorSerializable
        {
            get { return Serialize.BrushToString(FirstBreakoutDownUpMarkerColor); }
            set { FirstBreakoutDownUpMarkerColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Texte FBDU", Description = "Couleur du texte du signal", Order = 24, GroupName = "Signal FirstBreakoutDownUp")]
        public Brush FirstBreakoutDownUpTextColor { get; set; }
        
        [Browsable(false)]
        public string FirstBreakoutDownUpTextColorSerializable
        {
            get { return Serialize.BrushToString(FirstBreakoutDownUpTextColor); }
            set { FirstBreakoutDownUpTextColor = Serialize.StringToBrush(value); }
        }
        
        // Propriétés Signal FirstBreakoutDownUpStd2 avec DOUBLE FILTRE HORAIRE
        [NinjaScriptProperty]
        [Display(Name = "Afficher Signal FBDU2", Description = "Active le signal FBDU2", Order = 1, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public bool ShowFirstBreakoutDownUpStd2Signal { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Exiger Close > StdDev+2", Description = "Exige que la clôture soit au-dessus de StdDev+2", Order = 2, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public bool RequireCloseAboveStdDev2 { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 1 FBDU2 (Ticks)", Description = "Nombre de ticks pour le premier target", Order = 3, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public int FirstBreakoutDownUpStd2TargetTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 2 FBDU2 (Ticks)", Description = "Nombre de ticks pour le deuxième target", Order = 4, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public int FirstBreakoutDownUpStd2Target2Ticks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss FBDU2 (Ticks)", Description = "Nombre de ticks pour le stop loss", Order = 5, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public int FirstBreakoutDownUpStd2StopLossTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Nombre de Contrats FBDU2", Description = "Nombre total de contrats pour FBDU2", Order = 6, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public int FirstBreakoutDownUpStd2Contracts { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Contrats Target 1 FBDU2", Description = "Nombre de contrats à sortir au premier target", Order = 7, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public int FirstBreakoutDownUpStd2Target1Contracts { get; set; }
        
        // Premier filtre horaire FBDU2
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Horaire 1 FBDU2", Description = "Active le premier filtre horaire pour FBDU2", Order = 8, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public bool FirstBreakoutDownUpStd2EnableTimeFilter { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Début 1 FBDU2", Description = "Heure de début pour la première plage FBDU2", Order = 9, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public DateTime FirstBreakoutDownUpStd2StartTime { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Fin 1 FBDU2", Description = "Heure de fin pour la première plage FBDU2", Order = 10, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public DateTime FirstBreakoutDownUpStd2EndTime { get; set; }
        
        // Deuxième filtre horaire FBDU2
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Horaire 2 FBDU2", Description = "Active le deuxième filtre horaire pour FBDU2", Order = 11, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public bool FirstBreakoutDownUpStd2EnableTimeFilter2 { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Début 2 FBDU2", Description = "Heure de début pour la deuxième plage FBDU2", Order = 12, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public DateTime FirstBreakoutDownUpStd2StartTime2 { get; set; }
        
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Heure Fin 2 FBDU2", Description = "Heure de fin pour la deuxième plage FBDU2", Order = 13, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public DateTime FirstBreakoutDownUpStd2EndTime2 { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Trail Stop FBDU2", Description = "Active le trail stop pour FBDU2", Order = 14, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public bool FirstBreakoutDownUpStd2UseTrailStop { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trail Stop Distance FBDU2 (Ticks)", Description = "Distance du trail stop en ticks", Order = 15, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public int FirstBreakoutDownUpStd2TrailStopDistance { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Parabolic Stop FBDU2", Description = "Active le parabolic stop pour FBDU2", Order = 16, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public bool FirstBreakoutDownUpStd2UseParabolicStop { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration FBDU2", Description = "Facteur d'accélération initial", Order = 17, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public double FirstBreakoutDownUpStd2ParabolicAcceleration { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration Max FBDU2", Description = "Facteur d'accélération maximum", Order = 18, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public double FirstBreakoutDownUpStd2ParabolicAccelerationMax { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.001, 1)]
        [Display(Name = "Parabolic Acceleration Step FBDU2", Description = "Incrément d'accélération", Order = 19, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public double FirstBreakoutDownUpStd2ParabolicAccelerationStep { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Utiliser Break-Even FBDU2", Description = "Active le break-even après le premier target", Order = 20, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public bool FirstBreakoutDownUpStd2UseBreakEven { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Break-Even Offset FBDU2 (Ticks)", Description = "Offset en ticks au-dessus du prix d'entrée pour le break-even", Order = 21, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public int FirstBreakoutDownUpStd2BreakEvenOffset { get; set; }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Marqueur FBDU2", Description = "Couleur de la flèche du signal", Order = 22, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public Brush FirstBreakoutDownUpStd2MarkerColor { get; set; }
        
        [Browsable(false)]
        public string FirstBreakoutDownUpStd2MarkerColorSerializable
        {
            get { return Serialize.BrushToString(FirstBreakoutDownUpStd2MarkerColor); }
            set { FirstBreakoutDownUpStd2MarkerColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Texte FBDU2", Description = "Couleur du texte du signal", Order = 23, GroupName = "Signal FirstBreakoutDownUpStd2")]
        public Brush FirstBreakoutDownUpStd2TextColor { get; set; }
        
        [Browsable(false)]
        public string FirstBreakoutDownUpStd2TextColorSerializable
        {
            get { return Serialize.BrushToString(FirstBreakoutDownUpStd2TextColor); }
            set { FirstBreakoutDownUpStd2TextColor = Serialize.StringToBrush(value); }
        }
        
        // Propriétés de couleurs générales
        [XmlIgnore]
        [Display(Name = "Couleur Breakout UP", Description = "Couleur pour le breakout haussier", Order = 7, GroupName = "Couleurs")]
        public Brush UpBreakoutColor
        { get; set; }
        
        [Browsable(false)]
        public string UpBreakoutColorSerializable
        {
            get { return Serialize.BrushToString(UpBreakoutColor); }
            set { UpBreakoutColor = Serialize.StringToBrush(value); }
        }
        
        [XmlIgnore]
        [Display(Name = "Couleur Ligne Swing", Description = "Couleur de la ligne du swing high", Order = 8, GroupName = "Couleurs")]
        public Brush SwingHighLineColor
        { get; set; }
        
        [Browsable(false)]
        public string SwingHighLineColorSerializable
        {
            get { return Serialize.BrushToString(SwingHighLineColor); }
            set { SwingHighLineColor = Serialize.StringToBrush(value); }
        }
        
        [XmlIgnore]
        [Display(Name = "Couleur Swing Low", Description = "Couleur du marqueur swing low", Order = 9, GroupName = "Couleurs")]
        public Brush SwingLowColor
        { get; set; }
        
        [Browsable(false)]
        public string SwingLowColorSerializable
        {
            get { return Serialize.BrushToString(SwingLowColor); }
            set { SwingLowColor = Serialize.StringToBrush(value); }
        }
        
        [XmlIgnore]
        [Display(Name = "Couleur Cassures Rejetées", Description = "Couleur pour marquer les cassures rejetées", Order = 10, GroupName = "Couleurs")]
        public Brush RejectedBreakoutColor
        { get; set; }
        
        [Browsable(false)]
        public string RejectedBreakoutColorSerializable
        {
            get { return Serialize.BrushToString(RejectedBreakoutColor); }
            set { RejectedBreakoutColor = Serialize.StringToBrush(value); }
        }
        
        [Range(1, int.MaxValue)]
        [Display(Name = "Taille Marqueur", Description = "Taille des marqueurs de breakout", Order = 11, GroupName = "Couleurs")]
        public int MarkerSize
        { get; set; }
        
        [Range(1, int.MaxValue)]
        [Display(Name = "Largeur Ligne", Description = "Largeur des lignes", Order = 12, GroupName = "Couleurs")]
        public int LineWidth
        { get; set; }
        
        // Properties pour les Series liées aux plots
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> CurrentSwingHigh { get { return Values[0]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> BreakoutLevel { get { return Values[1]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> WaveSwingLow { get { return Values[2]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> WaveSizeTicks { get { return Values[3]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VWAP { get { return Values[4]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDevP1 { get { return Values[5]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDevM1 { get { return Values[6]; } }
        
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDevP2 { get { return Values[7]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDevM2 { get { return Values[8]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> CWAP { get { return Values[9]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> CwapStdDevP1 { get { return Values[10]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> CwapStdDevM1 { get { return Values[11]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> CwapStdDevP2 { get { return Values[12]; } }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> CwapStdDevM2 { get { return Values[13]; } }
        
        #endregion
    }
}