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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//Ce script est une version modifiée pour inclure un VWAP et CWAP ancrés au swing low de la vague de cassure.
namespace NinjaTrader.NinjaScript.Indicators
{
    public class aaaSBVCfbV50Codex : Indicator
    {
        // Variables private
		//
		private int consecutiveBarsInValueArea = 0; // Compteur de barres consécutives dans la Value Area
        private Swing swingIndicator;
        private double lastSwingHigh = 0;
        private int lastSwingHighBar = -1;
        private bool swingHighBroken = false;
		private int offsetTicksVwap = 2; // Offset pour la cassure du VWAP
		private bool firstBreakoutDownUpStd2Triggered = false;
        
        // Variables pour le signal firstBuy
        private bool firstBuyTriggered = false; // Flag pour s'assurer qu'on ne déclenche qu'une fois
        
        // Variables pour le nouveau signal firstbreakoutdownup
        private bool firstBreakoutDownUpTriggered = false; // Flag pour s'assurer qu'on ne déclenche qu'une fois
        
        // Variables pour le swing low de la vague
        private double currentWaveSwingLow = 0;
        private int currentWaveSwingLowBar = -1;
        private double waveSize = 0;
        
        // Variables pour le VWAP Ancré
        private int vwapAnchorBarIndex = -1; // Stocke l'index absolu de la barre d'ancre
		private bool vwapSequenceActive = false; // Gère l'état de la séquence VWAP
        
        // Paramètres
        private int strength = 3;
        private int lookBackPeriod = 50;
        private int offsetTicks = 2;
        private bool paintBars = true;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Marque les cassures de swing high et trace un VWAP/CWAP ancré depuis le swing low de la vague.";
                Name = "aaaSBVCfbV50Codex";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                // Valeurs par défaut des paramètres
                Strength = 5;
                LookBackPeriod = 50;
                OffsetTicks = 2;
                PaintBars = true;
                ShowSwingLowMarker = true;
                
                // PARAMÈTRES DE VALIDATION DE CASSURE
                MinimumSwingDistanceTicks = 30; // Valeur par défaut : 10 ticks minimum
                EnableMinimumDistance = true; // Activer/désactiver la validation de distance
                
                // NOUVEAU PARAMÈTRE : Qualité de la montée
                EnableUpBarFilter = true; // Activer/désactiver le filtre de qualité
                MaxDownBarsAllowed = 4; // Par défaut : 0 = que des barres up
                
                // NOUVEAU FILTRE : Nombre de barres total
                EnableBarCountFilter = true; // Activer/désactiver le filtre de nombre de barres
                MinBarsRequired = 4; // Minimum 4 barres
                MaxBarsAllowed = 10; // Maximum 10 barres
                
                ShowRejectedBreakouts = true; // Afficher les cassures rejetées
                
                // NOUVEAU FILTRE : Barre de cassure
                EnableBreakoutBarFilter = true; // Activer/désactiver le filtre de la barre de cassure
                MinBreakoutBarSizeTicks = 2; // Taille minimale de la barre de cassure en ticks
                RequireCloseAbovePreHigh = true; // Exiger que le close soit > high de la barre précédente
                EnableWaveSizeOverride = false; // Désactiver la validation par extension de swing par défaut
                WaveSizeOverrideTicks = 50; // Nombre de ticks requis pour valider la séquence sans cassure

                // Couleurs par défaut
                UpBreakoutColor = Brushes.Lime;
                SwingHighLineColor = Brushes.DodgerBlue;
                SwingLowColor = Brushes.Orange;
                RejectedBreakoutColor = Brushes.Gray; // Nouvelle couleur pour les cassures rejetées
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
                
                // Paramètres pour firstBuy Signal
                ShowFirstBuySignal = true;
                FirstBuyOffsetTicks = 1;
                FirstBuyMarkerColor = Brushes.Gold;
                FirstBuyTextColor = Brushes.White;
                FirstBuyTargetTicks = 5;  // NOUVEAU : Target par défaut
                FirstBuyStopLossTicks = 10;  // NOUVEAU : Stop Loss par défaut
                
                // Paramètres pour le nouveau signal firstbreakoutdownup
                ShowFirstBreakoutDownUpSignal = true;
				FirstBreakoutDownUpZoneOffsetTicks = 2;  // NOUVEAU : Offset pour étendre la zone vers le bas
				RequireCloseAboveStdDev1 = false;  // NOUVEAU : Option pour exiger close > StdDev+1
                FirstBreakoutDownUpMarkerColor = Brushes.Aqua;
                FirstBreakoutDownUpTextColor = Brushes.White;
                FirstBreakoutDownUpTargetTicks = 5;  // NOUVEAU : Target par défaut
                FirstBreakoutDownUpStopLossTicks = 10;  // NOUVEAU : Stop Loss par défaut
				
				// Paramètres pour le nouveau signal FirstBreakoutDownUpStd2
				ShowFirstBreakoutDownUpStd2Signal = true;
				RequireCloseAboveStdDev2 = false;  // NOUVEAU : Option pour exiger close > StdDev+2
				FirstBreakoutDownUpStd2MarkerColor = Brushes.Purple;
				FirstBreakoutDownUpStd2TextColor = Brushes.White;
				FirstBreakoutDownUpStd2TargetTicks = 8;  // Target par défaut plus élevé
				FirstBreakoutDownUpStd2StopLossTicks = 12;  // Stop Loss par défaut plus élevé
                
                // NOUVEAU : Paramètres pour le signal BarreBreak
                ShowBarreBreakSignal = true;
                BarreBreakTargetTicks = 6;
                BarreBreakStopLossTicks = 8;
                BarreBreakMarkerColor = Brushes.Yellow;
                BarreBreakTextColor = Brushes.Black;
                
                // Plots pour affichage des valeurs
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
		
		//
        protected override void OnBarUpdate()
		{
			if (CurrentBar < Math.Max(Strength * 2, LookBackPeriod))
				return;
		
			try
			{
				// **NOUVELLE LOGIQUE : Réinitialisation à chaque nouvelle session**
				if (Bars.IsFirstBarOfSession)
				{
					// Réinitialiser toutes les variables liées au VWAP et aux swings
					lastSwingHigh = 0;
					lastSwingHighBar = -1;
					swingHighBroken = false;
					currentWaveSwingLow = 0;
					currentWaveSwingLowBar = -1;
					waveSize = 0;
					vwapAnchorBarIndex = -1;
					vwapSequenceActive = false; // Réinitialiser l'état de la séquence
					consecutiveBarsInValueArea = 0;
					firstBuyTriggered = false; // Réinitialiser le signal firstBuy
					firstBreakoutDownUpTriggered = false; // Réinitialiser le nouveau signal
					firstBreakoutDownUpStd2Triggered = false; // Réinitialiser le nouveau signal Std2
					
					// Nettoyer les plots VWAP et CWAP pour éviter les continuités entre sessions
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
					
					Print($"Nouvelle session détectée - Réinitialisation des variables VWAP/CWAP à {Time[0]}");
				}
		
				// *** VÉRIFIER LA CASSURE DU VWAP À LA BAISSE ***
				// *** VÉRIFIER LES CONDITIONS DE SORTIE DE LA SÉQUENCE VWAP ***
				if (vwapSequenceActive && ShowVwap && CurrentBar > 0)
				{
					// Vérifier que le VWAP existe et est valide
					if (!double.IsNaN(VWAP[1])) // Utiliser VWAP[1] car VWAP[0] n'est pas encore calculé
					{
						// CONDITION 1: Cassure du VWAP à la baisse
						double vwapBreakLevel = VWAP[1] - (OffsetTicksVwap * TickSize);
						
						if (Close[0] < vwapBreakLevel)
						{
							Print($"VWAP cassé à la baisse à {Time[0]} - Close: {Close[0]}, VWAP Break Level: {vwapBreakLevel} - Fin de la séquence");
							
							// Marquer le point de cassure avec une flèche
							if (ShowVwapBreakMarker)
							{
								Draw.ArrowDown(this, "VwapBreak_" + CurrentBar, false, 0, High[0] + (2 * TickSize), VwapBreakColor);
								Draw.Text(this, "VwapBreakText_" + CurrentBar, "VWAP Break", 0, High[0] + (5 * TickSize), VwapBreakColor);
							}
							
							// Fin de la séquence VWAP
							EndVwapSequence();
							return; // Sortir pour éviter de recalculer le VWAP cette barre
						}
						
						// CONDITION 2: Barres consécutives dans la Value Area
						if (EnableValueAreaExit && !double.IsNaN(StdDevM1[1]) && !double.IsNaN(StdDevP1[1]))
						{
							// Vérifier si Open ET Close sont dans la Value Area (entre StdDev -1 et StdDev +1)
							bool openInValueArea = Open[0] >= StdDevM1[1] && Open[0] <= StdDevP1[1];
							bool closeInValueArea = Close[0] >= StdDevM1[1] && Close[0] <= StdDevP1[1];
							
							if (openInValueArea && closeInValueArea)
							{
								consecutiveBarsInValueArea++;
								Print($"Barre {CurrentBar} dans Value Area - Compteur: {consecutiveBarsInValueArea}/{BarsInValueAreaForExit}");
								
								// Si le nombre requis de barres consécutives est atteint
								if (consecutiveBarsInValueArea >= BarsInValueAreaForExit)
								{
									Print($"Consolidation détectée dans Value Area à {Time[0]} - {consecutiveBarsInValueArea} barres consécutives - Fin de la séquence");
									
									// Marquer la fin de séquence pour consolidation
									if (ShowVwapBreakMarker)
									{
										Draw.Square(this, "VAConsolidation_" + CurrentBar, false, 0, (StdDevP1[1] + StdDevM1[1]) / 2, Brushes.Yellow);
										Draw.Text(this, "VAConsolidationText_" + CurrentBar, "VA Exit", 0, StdDevP1[1] + (3 * TickSize), Brushes.Yellow);
									}
									
									// Fin de la séquence VWAP
									EndVwapSequence();
									return;
								}
							}
							else
							{
								// Réinitialiser le compteur si la barre n'est pas entièrement dans la Value Area
								consecutiveBarsInValueArea = 0;
							}
						}
					}
				}

		
				int swingHighBarsAgo = swingIndicator.SwingHighBar(0, 1, LookBackPeriod);
				
				if (swingHighBarsAgo > 0)
				{
					double currentSwingHigh = High[swingHighBarsAgo];
					double breakoutLevel = currentSwingHigh + (OffsetTicks * TickSize);
					
					CurrentSwingHigh[0] = currentSwingHigh;
					BreakoutLevel[0] = breakoutLevel;
					
					// Ne mettre à jour le swing high que si on n'est pas dans une séquence VWAP active
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
					
			// Détecter une nouvelle cassure ou une validation par extension uniquement si pas de séquence active
					if (!swingHighBroken && !vwapSequenceActive)
					{
						bool breakoutAttempt = Close[0] > breakoutLevel;
						int searchStart = Math.Min(swingHighBarsAgo, LookBackPeriod);
						double lowestPrice = double.MaxValue;
						int lowestBar = -1;

						// Recherche du swing low
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

						double waveAdvanceTicks = 0;
						bool overrideAttempt = false;

						if (EnableWaveSizeOverride && lowestBar >= 0 && lowestPrice > 0 && lowestPrice < double.MaxValue)
						{
							waveAdvanceTicks = Math.Round((Close[0] - lowestPrice) / TickSize);
							if (waveAdvanceTicks >= WaveSizeOverrideTicks)
								overrideAttempt = true;
						}

						if (breakoutAttempt || overrideAttempt)
						{
							bool triggeredByOverride = overrideAttempt && !breakoutAttempt;
							bool breakoutBarValid = true;

							if (breakoutAttempt && EnableBreakoutBarFilter)
							{
								// 1. Vérifier que ce n'est pas un doji (Close doit être différent de Open)
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

								// 2. Vérifier la taille minimale de la barre
								double barSize = Math.Abs(Close[0] - Open[0]);
								double barSizeTicks = Math.Round(barSize / TickSize);

								if (barSizeTicks < MinBreakoutBarSizeTicks)
								{
									breakoutBarValid = false;
									Print($"Cassure REJETÉE à {Time[0]} - Barre trop petite: {barSizeTicks} < {MinBreakoutBarSizeTicks} ticks");

									if (ShowRejectedBreakouts)
									{
										Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
										Draw.Text(this, "RejectedReason_" + CurrentBar, $"Size:{barSizeTicks}t<{MinBreakoutBarSizeTicks}t", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
									}
									return;
								}

								// 3. Vérifier que le Close est au-dessus du High de la barre précédente
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

							if (breakoutAttempt && !breakoutBarValid)
								return;

							if (lowestBar < 0 || lowestPrice == double.MaxValue || lowestPrice <= 0)
								return;

							// *** FILTRE 1 : VALIDATION DE LA DISTANCE MINIMALE ***
							bool distanceValid = true;
							double distanceInTicks = 0;

							if (EnableMinimumDistance && lowestPrice > 0)
							{
								distanceInTicks = Math.Round((currentSwingHigh - lowestPrice) / TickSize);
								Print($"Distance Swing: {distanceInTicks} ticks (Min requis: {MinimumSwingDistanceTicks})");

								if (distanceInTicks < MinimumSwingDistanceTicks)
								{
									distanceValid = false;
									// Cassure rejetée car distance insuffisante
									Print($"Cassure REJETÉE à {Time[0]} - Distance insuffisante: {distanceInTicks} < {MinimumSwingDistanceTicks} ticks");

									if (ShowRejectedBreakouts)
									{
										// Marquer la cassure rejetée
										Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
										Draw.Text(this, "RejectedReason_" + CurrentBar, $"Dist:{distanceInTicks}t", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
									}

									return; // Sortir sans valider la cassure
								}
							}

							// *** FILTRE 2 : VALIDATION DE LA QUALITÉ DE LA MONTÉE ***
							bool qualityValid = true;
							int downBarsCount = 0;

							if (EnableUpBarFilter && lowestBar > 0)
							{
								// Compter les barres down entre le swing low et la barre actuelle
								for (int i = lowestBar - 1; i >= 0; i--)
								{
									// Une barre est considérée comme "down" si Close < Open
									if (Close[i] < Open[i])
									{
										downBarsCount++;
									}
								}

								Print($"Nombre de barres down dans la montée: {downBarsCount} (Max autorisé: {MaxDownBarsAllowed})");

								if (downBarsCount > MaxDownBarsAllowed)
								{
									qualityValid = false;
									// Cassure rejetée car trop de barres down
									Print($"Cassure REJETÉE à {Time[0]} - Trop de barres down: {downBarsCount} > {MaxDownBarsAllowed}");

									if (ShowRejectedBreakouts)
									{
										// Marquer la cassure rejetée pour qualité
										Draw.Text(this, "RejectedBreakout_" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
										Draw.Text(this, "RejectedReason_" + CurrentBar, $"Down:{downBarsCount}/{MaxDownBarsAllowed}", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
									}

									return; // Sortir sans valider la cassure
								}
							}

							// *** FILTRE 3 : VALIDATION DU NOMBRE TOTAL DE BARRES ***
							bool barCountValid = true;
							int totalBarsCount = 0;

							if (EnableBarCountFilter && lowestBar > 0)
							{
								// Compter le nombre total de barres entre le swing low et la barre actuelle
								totalBarsCount = lowestBar; // lowestBar représente le nombre de barres depuis la barre actuelle

								Print($"Nombre total de barres: {totalBarsCount} (Min: {MinBarsRequired}, Max: {MaxBarsAllowed})");

								if (totalBarsCount < MinBarsRequired)
								{
									barCountValid = false;
									// Cassure rejetée car montée trop rapide
									Print($"Cassure REJETÉE à {Time[0]} - Montée trop rapide: {totalBarsCount} < {MinBarsRequired} barres");

									if (ShowRejectedBreakouts)
									{
										Draw.Text(this, "RejectedBreakout" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
										Draw.Text(this, "RejectedReason" + CurrentBar, $"Bars: {totalBarsCount}<{MinBarsRequired}", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
									}

									return; // Sortir sans valider la cassure
								}
								else if (totalBarsCount > MaxBarsAllowed)
								{
									barCountValid = false;
									// Cassure rejetée car montée trop lente
									Print($"Cassure REJETÉE à {Time[0]} - Montée trop lente: {totalBarsCount} > {MaxBarsAllowed} barres");

									if (ShowRejectedBreakouts)
									{
										Draw.Text(this, "RejectedBreakout" + CurrentBar, "✗", 0, High[0] + (2 * TickSize), RejectedBreakoutColor);
										Draw.Text(this, "RejectedReason" + CurrentBar, $"Bars: {totalBarsCount}>{MaxBarsAllowed}={swingHighBarsAgo}-{lowestBar}", 0, High[0] + (5 * TickSize), RejectedBreakoutColor);
									}

									return; // Sortir sans valider la cassure
								}
							}

							// *** CASSURE VALIDÉE - Les trois filtres sont passés ***
							if (distanceValid && qualityValid && barCountValid)
							{
								swingHighBroken = true;
								vwapSequenceActive = true; // Activer la séquence VWAP
								consecutiveBarsInValueArea = 0;
								firstBuyTriggered = false; // Réinitialiser pour la nouvelle séquence
								firstBreakoutDownUpTriggered = false; // Réinitialiser le nouveau signal
								firstBreakoutDownUpStd2Triggered = false; // Réinitialiser pour la nouvelle séquence

								currentWaveSwingLow = lowestPrice;
								currentWaveSwingLowBar = lowestBar;

								if (triggeredByOverride)
									Print($"Séquence validée par extension de swing ({waveAdvanceTicks} ticks >= {WaveSizeOverrideTicks}) à {Time[0]}");

								// *** NOUVEAU SIGNAL BARREBREAK (BB) ***
								if (ShowBarreBreakSignal)
								{
									// Prix d'entrée = Close[0]
									double entryPrice = Close[0];
									double targetPrice = entryPrice + (BarreBreakTargetTicks * TickSize);
									double stopLossPrice = entryPrice - (BarreBreakStopLossTicks * TickSize);

									Print($"SIGNAL BARREBREAK à {Time[0]} - Entry: {entryPrice:F2}, Target: {targetPrice:F2} (+{BarreBreakTargetTicks}t), Stop: {stopLossPrice:F2} (-{BarreBreakStopLossTicks}t)");

									// Dessiner le signal visuel
									Draw.ArrowUp(this, "BarreBreak_" + CurrentBar, false, 0, Low[0] - (2 * TickSize), BarreBreakMarkerColor);
									Draw.Text(this, "BarreBreakText_" + CurrentBar, "BB", 0, Low[0] - (5 * TickSize), BarreBreakTextColor);

									// Dessiner les niveaux Target et Stop Loss
									// Target
									Draw.Line(this, "BarreBreakTarget_" + CurrentBar, false, 0, targetPrice, -5, targetPrice, Brushes.Lime, DashStyleHelper.Dot, 2);
									Draw.Text(this, "BarreBreakTargetText_" + CurrentBar, $"T: {targetPrice:F2} (+{BarreBreakTargetTicks}t)", -5, targetPrice, Brushes.Lime);

									// Stop Loss
									Draw.Line(this, "BarreBreakStopLoss_" + CurrentBar, false, 0, stopLossPrice, -5, stopLossPrice, Brushes.OrangeRed, DashStyleHelper.Dot, 2);
									Draw.Text(this, "BarreBreakStopLossText_" + CurrentBar, $"SL: {stopLossPrice:F2} (-{BarreBreakStopLossTicks}t)", -5, stopLossPrice, Brushes.OrangeRed);

									// Entry level
									Draw.Dot(this, "BarreBreakEntry_" + CurrentBar, false, 0, entryPrice, Brushes.Yellow);
								}

								if (currentWaveSwingLow > 0)
								{
									DateTime swingLowTime = Time[currentWaveSwingLowBar];
									DateTime currentTime = Time[0];

									string validationLabel = triggeredByOverride ? "Séquence VALIDÉE (extension)" : "Cassure VALIDÉE";
									Print($"{validationLabel} à {Time[0]} - Distance: {distanceInTicks} ticks, Barres down: {downBarsCount}/{MaxDownBarsAllowed}, Total barres: {totalBarsCount}");

									if (swingLowTime.Date != currentTime.Date)
									{
										Print($"Swing low détecté dans une session différente ({swingLowTime.Date} vs {currentTime.Date}) - VWAP/CWAP non calculé");
										currentWaveSwingLow = 0;
										currentWaveSwingLowBar = -1;
										vwapSequenceActive = false; // Désactiver la séquence
									}
									else
									{
										vwapAnchorBarIndex = CurrentBar - currentWaveSwingLowBar;

										// Calcul et traçage rétroactif du VWAP et CWAP (Backfill)
										if (ShowVwap || ShowCwap)
										{
											// Variables pour VWAP
											double runningSumPriceVolume = 0;
											double runningSumVolume = 0;
											List<Tuple<double, double>> priceVolumeData = new List<Tuple<double, double>>();

											// Variables pour CWAP
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

												// Pour VWAP
												runningSumPriceVolume += typicalPrice * volume;
												runningSumVolume += volume;
												priceVolumeData.Add(new Tuple<double, double>(typicalPrice, volume));

												// Pour CWAP
												runningSumPrice += typicalPrice;
												barCount++;
												priceData.Add(typicalPrice);

												// Calcul VWAP
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

												// Calcul CWAP
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
									}

									waveSize = Math.Round((Close[0] - currentWaveSwingLow) / TickSize);
									WaveSwingLow[0] = currentWaveSwingLow;
									WaveSizeTicks[0] = waveSize;

									if (ShowSwingLowMarker)
									{
										// Afficher les trois métriques de validation sur le marqueur
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
				// *** CALCUL CONTINU DU VWAP ET CWAP (seulement si la séquence est active) ***
				if (vwapSequenceActive && (ShowVwap || ShowCwap) && vwapAnchorBarIndex != -1 && (CurrentBar > vwapAnchorBarIndex))
				{
					DateTime anchorTime = Time[CurrentBar - vwapAnchorBarIndex];
					DateTime currentTime = Time[0];
					
					if (anchorTime.Date == currentTime.Date)
					{
						int barsToCalculate = CurrentBar - vwapAnchorBarIndex;
						
						// Variables pour VWAP
						double sumPriceVolume = 0;
						double sumVolume = 0;
						
						// Variables pour CWAP
						double sumPrice = 0;
						int barCount = 0;
		
						// Premier passage pour calculer les sommes
						for (int i = 0; i <= barsToCalculate; ++i)
						{
							if (Time[i].Date != currentTime.Date)
								continue;
								
							double typicalPrice = (High[i] + Low[i] + Close[i]) / 3.0;
							double volume = Volume[i];
							
							// Pour VWAP
							sumPriceVolume += typicalPrice * volume;
							sumVolume += volume;
							
							// Pour CWAP
							sumPrice += typicalPrice;
							barCount++;
						}
					
						// Calcul VWAP
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
						
						// Calcul CWAP
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
						
						// Nettoyer tous les plots
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
				
				// *** DÉTECTION DU SIGNAL FIRSTBUY ***
				if (vwapSequenceActive && ShowFirstBuySignal && !firstBuyTriggered && ShowVwap)
				{
					// Vérifier que les valeurs VWAP et StdDev+1 existent et sont valides
					if (!double.IsNaN(VWAP[0]) && !double.IsNaN(StdDevP1[0]))
					{
						// NOUVELLE CONDITION 1 : Ne pas être sur la barre de cassure
						// On vérifie qu'on n'est pas sur la barre qui vient de casser le swing high
						bool isBreakoutBar = (swingHighBroken && currentWaveSwingLowBar == 0);
						
						// NOUVELLE CONDITION 2 : La barre précédente doit être une barre UP
						// Cela évite le chevauchement avec FBDU qui nécessite une barre down/doji précédente
						bool previousBarIsUp = CurrentBar > 0 && Close[1] > Open[1];
						
						// Conditions originales pour firstBuy :
						// 1. Low dans la zone entre VWAP et StdDev+1
						bool lowInZone = Low[0] >= VWAP[0] && Low[0] <= StdDevP1[0];
						
						// 2. Close au-dessus de StdDev+1 + offset
						double firstBuyLevel = StdDevP1[0] + (FirstBuyOffsetTicks * TickSize);
						bool closeAboveLevel = Close[0] > firstBuyLevel;
						
						// Si toutes les conditions sont remplies (incluant les nouvelles)
						if (!isBreakoutBar && previousBarIsUp && lowInZone && closeAboveLevel)
						{
							firstBuyTriggered = true; // Marquer comme déclenché
							
							// Prix d'entrée = Close[0]
							double entryPrice = Close[0];
							double targetPrice = entryPrice + (FirstBuyTargetTicks * TickSize);
							double stopLossPrice = entryPrice - (FirstBuyStopLossTicks * TickSize);
							
							Print($"SIGNAL FIRSTBUY à {Time[0]} - Entry: {entryPrice:F2}, Target: {targetPrice:F2} (+{FirstBuyTargetTicks}t), Stop: {stopLossPrice:F2} (-{FirstBuyStopLossTicks}t)");
							
							// Dessiner le signal visuel
							Draw.ArrowUp(this, "FirstBuy_" + CurrentBar, false, 0, Low[0] - (2 * TickSize), FirstBuyMarkerColor);
							Draw.Text(this, "FirstBuyText_" + CurrentBar, "Buy", 0, Low[0] - (5 * TickSize), FirstBuyTextColor);
							
							// Dessiner les niveaux Target et Stop Loss
							// Target
							Draw.Line(this, "FirstBuyTarget_" + CurrentBar, false, 0, targetPrice, -5, targetPrice, Brushes.Green, DashStyleHelper.Dot, 2);
							Draw.Text(this, "FirstBuyTargetText_" + CurrentBar, $"T: {targetPrice:F2} (+{FirstBuyTargetTicks}t)", -5, targetPrice, Brushes.Green);
							
							// Stop Loss
							Draw.Line(this, "FirstBuyStopLoss_" + CurrentBar, false, 0, stopLossPrice, -5, stopLossPrice, Brushes.Red, DashStyleHelper.Dot, 2);
							Draw.Text(this, "FirstBuyStopLossText_" + CurrentBar, $"SL: {stopLossPrice:F2} (-{FirstBuyStopLossTicks}t)", -5, stopLossPrice, Brushes.Red);
							
							// Entry level
							Draw.Dot(this, "FirstBuyEntry_" + CurrentBar, false, 0, entryPrice, Brushes.White);
							
							// Optionnel : Dessiner une zone pour montrer où le signal s'est déclenché
							Draw.Rectangle(this, "FirstBuyZone_" + CurrentBar, false, 5, VWAP[0], 0, StdDevP1[0], 
										Brushes.Gold, Brushes.Gold, 20);
						}
					}
				}
				
				// *** NOUVEAU : DÉTECTION DU SIGNAL FIRSTBREAKOUTDOWNUP ***
				if (vwapSequenceActive && ShowFirstBreakoutDownUpSignal && !firstBreakoutDownUpTriggered && ShowVwap && CurrentBar > 1)
				{
					// Vérifier que les valeurs VWAP et StdDev+1 existent et sont valides
					if (!double.IsNaN(VWAP[0]) && !double.IsNaN(StdDevP1[0]))
					{
						// Conditions pour firstbreakoutdownup :
						// 1. La barre précédente (index 1) doit être une barre DOWN ou DOJI (MODIFIÉ)
						bool previousBarIsDownOrDoji = Close[1] <= Open[1];  // CHANGÉ de < à <=
						
						// 2. La barre actuelle (index 0) doit être une barre UP ET clôturer au-dessus de StdDev+1
						bool currentBarIsUp = Close[0] > Open[0];
						
						// 2bis. MODIFIÉ : Close au-dessus de StdDev+1 (optionnel)
						bool closeAboveStdDev1 = !RequireCloseAboveStdDev1 || Close[0] >= StdDevP1[0];
						
						// 3. Le Low de la barre actuelle doit être dans la zone entre VWAP et StdDev+1 AVEC OFFSET
						double vwapWithOffset = VWAP[0] - (FirstBreakoutDownUpZoneOffsetTicks * TickSize);
						bool lowInTargetZone = Low[0] >= vwapWithOffset && Low[0] <= StdDevP1[0];
						
						// Si toutes les conditions sont remplies
						if (previousBarIsDownOrDoji && currentBarIsUp && lowInTargetZone && closeAboveStdDev1)
						{
							firstBreakoutDownUpTriggered = true; // Marquer comme déclenché
							
							// Prix d'entrée = Close[0]
							double entryPrice = Close[0];
							double targetPrice = entryPrice + (FirstBreakoutDownUpTargetTicks * TickSize);
							double stopLossPrice = entryPrice - (FirstBreakoutDownUpStopLossTicks * TickSize);
							
							Print($"SIGNAL FIRSTBREAKOUTDOWNUP à {Time[0]} - Entry: {entryPrice:F2}, Target: {targetPrice:F2} (+{FirstBreakoutDownUpTargetTicks}t), Stop: {stopLossPrice:F2} (-{FirstBreakoutDownUpStopLossTicks}t)");
							
							// Dessiner le signal visuel avec un style différent du firstBuy
							Draw.ArrowUp(this, "FirstBreakoutDownUp_" + CurrentBar, false, 0, Low[0] - (3 * TickSize), FirstBreakoutDownUpMarkerColor);
							Draw.Text(this, "FirstBreakoutDownUpText_" + CurrentBar, "FBDU", 0, Low[0] - (6 * TickSize), FirstBreakoutDownUpTextColor);
							
							// NOUVEAU : Dessiner les niveaux Target et Stop Loss
							// Target
							Draw.Line(this, "FBDUTarget_" + CurrentBar, false, 0, targetPrice, -5, targetPrice, Brushes.LightGreen, DashStyleHelper.Dot, 2);
							Draw.Text(this, "FBDUTargetText_" + CurrentBar, $"T: {targetPrice:F2} (+{FirstBreakoutDownUpTargetTicks}t)", -5, targetPrice, Brushes.LightGreen);
							
							// Stop Loss
							Draw.Line(this, "FBDUStopLoss_" + CurrentBar, false, 0, stopLossPrice, -5, stopLossPrice, Brushes.Pink, DashStyleHelper.Dot, 2);
							Draw.Text(this, "FBDUStopLossText_" + CurrentBar, $"SL: {stopLossPrice:F2} (-{FirstBreakoutDownUpStopLossTicks}t)", -5, stopLossPrice, Brushes.Pink);
							
							// Entry level
							Draw.Dot(this, "FBDUEntry_" + CurrentBar, false, 0, entryPrice, Brushes.Aqua);
							
							// Optionnel : Marquer aussi la barre précédente pour montrer le pattern
							string prevBarType = (Close[1] == Open[1]) ? "=" : "↓";  // Montrer si c'est un doji ou barre down
							Draw.Text(this, "PreviousBar_" + CurrentBar, prevBarType, 1, High[1] + (2 * TickSize), FirstBreakoutDownUpMarkerColor);
						}
					}
				}
				
				// *** NOUVEAU : DÉTECTION DU SIGNAL FIRSTBREAKOUTDOWNUPSTD2 ***
				if (vwapSequenceActive && ShowFirstBreakoutDownUpStd2Signal && !firstBreakoutDownUpStd2Triggered && ShowVwap && CurrentBar > 1)
				{
					// Vérifier que les valeurs StdDev+1 et StdDev+2 existent et sont valides
					if (!double.IsNaN(StdDevP1[0]) && !double.IsNaN(StdDevP2[0]))
					{
						// Conditions pour firstbreakoutdownupStd2 :
						// 1. La barre précédente (index 1) doit être une barre DOWN ou DOJI
						bool previousBarIsDownOrDoji = Close[1] <= Open[1];
						
						// 2. La barre actuelle (index 0) doit être une barre UP ET clôturer au-dessus de StdDev+2
						bool currentBarIsUp = Close[0] > Open[0];
						
						// 2bis. MODIFIÉ : Close au-dessus de StdDev+2 (optionnel)
						bool closeAboveStdDev2 = !RequireCloseAboveStdDev2 || Close[0] >= StdDevP2[0];
						
						// 3. Le Low de la barre actuelle doit être dans la zone entre StdDev+1 et StdDev+2
						bool lowInTargetZone = Low[0] >= StdDevP1[0] && Low[0] <= StdDevP2[0];
						
						// Si toutes les conditions sont remplies
						if (previousBarIsDownOrDoji && currentBarIsUp && lowInTargetZone && closeAboveStdDev2)
						{
							firstBreakoutDownUpStd2Triggered = true; // Marquer comme déclenché
							
							// Prix d'entrée = Close[0]
							double entryPrice = Close[0];
							double targetPrice = entryPrice + (FirstBreakoutDownUpStd2TargetTicks * TickSize);
							double stopLossPrice = entryPrice - (FirstBreakoutDownUpStd2StopLossTicks * TickSize);
							
							Print($"SIGNAL FIRSTBREAKOUTDOWNUPSTD2 à {Time[0]} - Entry: {entryPrice:F2}, Target: {targetPrice:F2} (+{FirstBreakoutDownUpStd2TargetTicks}t), Stop: {stopLossPrice:F2} (-{FirstBreakoutDownUpStd2StopLossTicks}t)");
							
							// Dessiner le signal visuel avec un style différent
							Draw.ArrowUp(this, "FirstBreakoutDownUpStd2_" + CurrentBar, false, 0, Low[0] - (3 * TickSize), FirstBreakoutDownUpStd2MarkerColor);
							Draw.Text(this, "FirstBreakoutDownUpStd2Text_" + CurrentBar, "FBDU2", 0, Low[0] - (6 * TickSize), FirstBreakoutDownUpStd2TextColor);
							
							// Dessiner les niveaux Target et Stop Loss
							// Target
							Draw.Line(this, "FBDU2Target_" + CurrentBar, false, 0, targetPrice, -5, targetPrice, Brushes.Plum, DashStyleHelper.Dot, 2);
							Draw.Text(this, "FBDU2TargetText_" + CurrentBar, $"T2: {targetPrice:F2} (+{FirstBreakoutDownUpStd2TargetTicks}t)", -5, targetPrice, Brushes.Plum);
							
							// Stop Loss
							Draw.Line(this, "FBDU2StopLoss_" + CurrentBar, false, 0, stopLossPrice, -5, stopLossPrice, Brushes.Violet, DashStyleHelper.Dot, 2);
							Draw.Text(this, "FBDU2StopLossText_" + CurrentBar, $"SL2: {stopLossPrice:F2} (-{FirstBreakoutDownUpStd2StopLossTicks}t)", -5, stopLossPrice, Brushes.Violet);
							
							// Entry level
							Draw.Dot(this, "FBDU2Entry_" + CurrentBar, false, 0, entryPrice, Brushes.Purple);
							
							// Marquer aussi la barre précédente pour montrer le pattern
							string prevBarType = (Close[1] == Open[1]) ? "=" : "↓";
							Draw.Text(this, "PreviousBarStd2_" + CurrentBar, prevBarType, 1, High[1] + (4 * TickSize), FirstBreakoutDownUpStd2MarkerColor);
							
							// Optionnel : Dessiner une zone pour montrer où le signal s'est déclenché (zone StdDev+1 à StdDev+2)
							Draw.Rectangle(this, "FBDU2Zone_" + CurrentBar, false, 5, StdDevP1[0], 0, StdDevP2[0], 
										Brushes.Purple, Brushes.Purple, 15);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Print("Erreur dans aaSwingBvwapGemini: " + ex.Message);
			}
		}
		//
		private void EndVwapSequence()
		{
			vwapSequenceActive = false;
			vwapAnchorBarIndex = -1;
			consecutiveBarsInValueArea = 0; // Réinitialiser le compteur
			firstBuyTriggered = false; // Réinitialiser le flag firstBuy
			firstBreakoutDownUpTriggered = false; // Réinitialiser le nouveau signal
			firstBreakoutDownUpStd2Triggered = false; // Réinitialiser le signal Std2
			
			// Réinitialiser les variables pour permettre une nouvelle séquence
			lastSwingHigh = 0;
			lastSwingHighBar = -1;
			swingHighBroken = false;
			currentWaveSwingLow = 0;
			currentWaveSwingLowBar = -1;
			waveSize = 0;
			
			// Effacer les plots VWAP et CWAP
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
        
        #region Properties
        
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
        
        // *** PARAMÈTRES POUR LA VALIDATION DE DISTANCE ***
        [NinjaScriptProperty]
        [Display(Name = "Activer Distance Minimale", Description = "Active la validation par distance minimale swing low-high", Order = 4, GroupName = "Validation Cassure")]
        public bool EnableMinimumDistance
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Distance Minimale (Ticks)", Description = "Distance minimale requise entre swing low et swing high", Order = 5, GroupName = "Validation Cassure")]
        public int MinimumSwingDistanceTicks
        { get; set; }
        
        // *** PARAMÈTRES POUR LA QUALITÉ DE LA MONTÉE ***
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Qualité Montée", Description = "Active le contrôle du nombre de barres down dans la montée", Order = 6, GroupName = "Validation Cassure")]
        public bool EnableUpBarFilter
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Max Barres Down Autorisées", Description = "Nombre maximum de barres down tolérées", Order = 7, GroupName = "Validation Cassure")]
        public int MaxDownBarsAllowed
        { get; set; }
        
        // *** NOUVEAU FILTRE : NOMBRE TOTAL DE BARRES ***
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
        
        // *** NOUVEAU FILTRE : VALIDATION DE LA BARRE DE CASSURE ***
        [NinjaScriptProperty]
        [Display(Name = "Activer Filtre Barre Cassure", Description = "Active le contrôle de la qualité de la barre de cassure", Order = 12, GroupName = "Validation Cassure")]
        public bool EnableBreakoutBarFilter
        { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Taille Min Barre Cassure (Ticks)", Description = "Taille minimale de la barre de cassure en ticks (0 = pas de minimum)", Order = 13, GroupName = "Validation Cassure")]
        public int MinBreakoutBarSizeTicks
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Close > High Précédent", Description = "Exiger que le close de la barre de cassure soit > au high de la barre précédente", Order = 14, GroupName = "Validation Cassure")]
        public bool RequireCloseAbovePreHigh
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Activer Validation par Extension", Description = "Valider la séquence si l'avancée du swing atteint un nombre de ticks défini", Order = 15, GroupName = "Validation Cassure")]
        public bool EnableWaveSizeOverride
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Extension minimale (Ticks)", Description = "Nombre de ticks requis pour valider la séquence sans casser le swing high", Order = 16, GroupName = "Validation Cassure")]
        public int WaveSizeOverrideTicks
        { get; set; }

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
        
        // --- SECTION : PROPRIETES VWAP ---
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
		
		// --- SECTION : PROPRIETES CWAP ---
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
        
        // --- FIN SECTION CWAP ---
		
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
        
        // --- FIN DE LA SECTION VWAP ---
        
        // --- SECTION : PROPRIETES BARREBREAK SIGNAL ---
        [NinjaScriptProperty]
        [Display(Name = "Afficher Signal BarreBreak", Description = "Active le signal BarreBreak (BB) sur la barre de cassure", Order = 1, GroupName = "Signal BarreBreak")]
        public bool ShowBarreBreakSignal { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target BarreBreak (Ticks)", Description = "Nombre de ticks pour le target", Order = 2, GroupName = "Signal BarreBreak")]
        public int BarreBreakTargetTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss BarreBreak (Ticks)", Description = "Nombre de ticks pour le stop loss", Order = 3, GroupName = "Signal BarreBreak")]
        public int BarreBreakStopLossTicks { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Marqueur BarreBreak", Description = "Couleur de la flèche du signal BarreBreak", Order = 4, GroupName = "Signal BarreBreak")]
        public Brush BarreBreakMarkerColor { get; set; }

        [Browsable(false)]
        public string BarreBreakMarkerColorSerializable
        {
            get { return Serialize.BrushToString(BarreBreakMarkerColor); }
            set { BarreBreakMarkerColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Texte BarreBreak", Description = "Couleur du texte du signal BarreBreak", Order = 5, GroupName = "Signal BarreBreak")]
        public Brush BarreBreakTextColor { get; set; }

        [Browsable(false)]
        public string BarreBreakTextColorSerializable
        {
            get { return Serialize.BrushToString(BarreBreakTextColor); }
            set { BarreBreakTextColor = Serialize.StringToBrush(value); }
        }
        // --- FIN SECTION BARREBREAK ---
        
        // --- SECTION : PROPRIETES FIRSTBUY SIGNAL ---
        [NinjaScriptProperty]
        [Display(Name = "Afficher Signal FirstBuy", Description = "Active le signal d'achat firstBuy", Order = 1, GroupName = "Signal FirstBuy")]
        public bool ShowFirstBuySignal { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Offset FirstBuy (Ticks)", Description = "Offset en ticks au-dessus de StdDev+1 pour confirmer le signal", Order = 2, GroupName = "Signal FirstBuy")]
        public int FirstBuyOffsetTicks { get; set; }
		
		// NOUVEAU : Target et Stop Loss pour FirstBuy
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target FirstBuy (Ticks)", Description = "Nombre de ticks pour le target", Order = 3, GroupName = "Signal FirstBuy")]
        public int FirstBuyTargetTicks { get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss FirstBuy (Ticks)", Description = "Nombre de ticks pour le stop loss", Order = 4, GroupName = "Signal FirstBuy")]
        public int FirstBuyStopLossTicks { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Marqueur FirstBuy", Description = "Couleur de la flèche du signal firstBuy", Order = 5, GroupName = "Signal FirstBuy")]
        public Brush FirstBuyMarkerColor { get; set; }

        [Browsable(false)]
        public string FirstBuyMarkerColorSerializable
        {
            get { return Serialize.BrushToString(FirstBuyMarkerColor); }
            set { FirstBuyMarkerColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Texte FirstBuy", Description = "Couleur du texte du signal firstBuy", Order = 6, GroupName = "Signal FirstBuy")]
        public Brush FirstBuyTextColor { get; set; }

        [Browsable(false)]
        public string FirstBuyTextColorSerializable
        {
            get { return Serialize.BrushToString(FirstBuyTextColor); }
            set { FirstBuyTextColor = Serialize.StringToBrush(value); }
        }
        // --- FIN SECTION FIRSTBUY ---
        
        // --- SECTION : PROPRIETES FIRSTBREAKOUTDOWNUP SIGNAL ---
        [NinjaScriptProperty]
        [Display(Name = "Afficher Signal FirstBreakoutDownUp", Description = "Active le signal firstbreakoutdownup", Order = 1, GroupName = "Signal FirstBreakoutDownUp")]
        public bool ShowFirstBreakoutDownUpSignal { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Exiger Close > StdDev+1", Description = "Exige que la clôture soit au-dessus de StdDev+1", Order = 2, GroupName = "Signal FirstBreakoutDownUp")]
		public bool RequireCloseAboveStdDev1 { get; set; }
		
		// NOUVEAU : Target et Stop Loss pour FirstBreakoutDownUp
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target FBDU (Ticks)", Description = "Nombre de ticks pour le target", Order = 2, GroupName = "Signal FirstBreakoutDownUp")]
        public int FirstBreakoutDownUpTargetTicks { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Offset Zone FBDU (Ticks)", Description = "Extension de la zone vers le bas en ticks", Order = 3, GroupName = "Signal FirstBreakoutDownUp")]
		public int FirstBreakoutDownUpZoneOffsetTicks { get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss FBDU (Ticks)", Description = "Nombre de ticks pour le stop loss", Order = 3, GroupName = "Signal FirstBreakoutDownUp")]
        public int FirstBreakoutDownUpStopLossTicks { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Marqueur FirstBreakoutDownUp", Description = "Couleur de la flèche du signal", Order = 4, GroupName = "Signal FirstBreakoutDownUp")]
        public Brush FirstBreakoutDownUpMarkerColor { get; set; }

        [Browsable(false)]
        public string FirstBreakoutDownUpMarkerColorSerializable
        {
            get { return Serialize.BrushToString(FirstBreakoutDownUpMarkerColor); }
            set { FirstBreakoutDownUpMarkerColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur Texte FirstBreakoutDownUp", Description = "Couleur du texte du signal", Order = 5, GroupName = "Signal FirstBreakoutDownUp")]
        public Brush FirstBreakoutDownUpTextColor { get; set; }

        [Browsable(false)]
        public string FirstBreakoutDownUpTextColorSerializable
        {
            get { return Serialize.BrushToString(FirstBreakoutDownUpTextColor); }
            set { FirstBreakoutDownUpTextColor = Serialize.StringToBrush(value); }
        }
        // --- FIN SECTION FIRSTBREAKOUTDOWNUP ---
		// --- SECTION : PROPRIÉTÉS FIRSTBREAKOUTDOWNUPSTD2 SIGNAL ---
		[NinjaScriptProperty]
		[Display(Name = "Afficher Signal FirstBreakoutDownUpStd2", Description = "Active le signal firstbreakoutdownupStd2", Order = 1, GroupName = "Signal FirstBreakoutDownUpStd2")]
		public bool ShowFirstBreakoutDownUpStd2Signal { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Exiger Close > StdDev+2", Description = "Exige que la clôture soit au-dessus de StdDev+2", Order = 2, GroupName = "Signal FirstBreakoutDownUpStd2")]
		public bool RequireCloseAboveStdDev2 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Target FBDU2 (Ticks)", Description = "Nombre de ticks pour le target", Order = 2, GroupName = "Signal FirstBreakoutDownUpStd2")]
		public int FirstBreakoutDownUpStd2TargetTicks { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Stop Loss FBDU2 (Ticks)", Description = "Nombre de ticks pour le stop loss", Order = 3, GroupName = "Signal FirstBreakoutDownUpStd2")]
		public int FirstBreakoutDownUpStd2StopLossTicks { get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Couleur Marqueur FirstBreakoutDownUpStd2", Description = "Couleur de la flèche du signal", Order = 4, GroupName = "Signal FirstBreakoutDownUpStd2")]
		public Brush FirstBreakoutDownUpStd2MarkerColor { get; set; }
		
		[Browsable(false)]
		public string FirstBreakoutDownUpStd2MarkerColorSerializable
		{
			get { return Serialize.BrushToString(FirstBreakoutDownUpStd2MarkerColor); }
			set { FirstBreakoutDownUpStd2MarkerColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Couleur Texte FirstBreakoutDownUpStd2", Description = "Couleur du texte du signal", Order = 5, GroupName = "Signal FirstBreakoutDownUpStd2")]
		public Brush FirstBreakoutDownUpStd2TextColor { get; set; }
		
		[Browsable(false)]
		public string FirstBreakoutDownUpStd2TextColorSerializable
		{
			get { return Serialize.BrushToString(FirstBreakoutDownUpStd2TextColor); }
			set { FirstBreakoutDownUpStd2TextColor = Serialize.StringToBrush(value); }
		}
		
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
        
        // PROPRIÉTÉ COULEUR
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
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> CurrentSwingHigh
        {
            get { return Values[0]; }
        }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> BreakoutLevel
        {
            get { return Values[1]; }
        }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> WaveSwingLow
        {
            get { return Values[2]; }
        }
        
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> WaveSizeTicks
        {
            get { return Values[3]; }
        }

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
        
        // Propriétés pour CWAP
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

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private aaaSBVCfbV50Codex[] cacheaaaSBVCfbV50Codex;
		public aaaSBVCfbV50Codex aaaSBVCfbV50Codex(int strength, int lookBackPeriod, int offsetTicks, bool enableMinimumDistance, int minimumSwingDistanceTicks, bool enableUpBarFilter, int maxDownBarsAllowed, bool enableBarCountFilter, int minBarsRequired, int maxBarsAllowed, bool showRejectedBreakouts, bool enableBreakoutBarFilter, int minBreakoutBarSizeTicks, bool requireCloseAbovePreHigh, bool enableWaveSizeOverride, int waveSizeOverrideTicks, bool paintBars, bool showSwingHighLine, bool showSwingLowMarker, bool showVwap, double stdDev1Multiplier, double stdDev2Multiplier, Brush vwapColor, Brush stdDev1BandColor, Brush stdDev2BandColor, int vwapLineWidth, bool showCwap, Brush cwapColor, Brush cwapStdDev1BandColor, Brush cwapStdDev2BandColor, int cwapLineWidth, int offsetTicksVwap, bool showVwapBreakMarker, bool enableValueAreaExit, int barsInValueAreaForExit, bool showBarreBreakSignal, int barreBreakTargetTicks, int barreBreakStopLossTicks, Brush barreBreakMarkerColor, Brush barreBreakTextColor, bool showFirstBuySignal, int firstBuyOffsetTicks, int firstBuyTargetTicks, int firstBuyStopLossTicks, Brush firstBuyMarkerColor, Brush firstBuyTextColor, bool showFirstBreakoutDownUpSignal, bool requireCloseAboveStdDev1, int firstBreakoutDownUpTargetTicks, int firstBreakoutDownUpZoneOffsetTicks, int firstBreakoutDownUpStopLossTicks, Brush firstBreakoutDownUpMarkerColor, Brush firstBreakoutDownUpTextColor, bool showFirstBreakoutDownUpStd2Signal, bool requireCloseAboveStdDev2, int firstBreakoutDownUpStd2TargetTicks, int firstBreakoutDownUpStd2StopLossTicks, Brush firstBreakoutDownUpStd2MarkerColor, Brush firstBreakoutDownUpStd2TextColor)
		{
			return aaaSBVCfbV50Codex(Input, strength, lookBackPeriod, offsetTicks, enableMinimumDistance, minimumSwingDistanceTicks, enableUpBarFilter, maxDownBarsAllowed, enableBarCountFilter, minBarsRequired, maxBarsAllowed, showRejectedBreakouts, enableBreakoutBarFilter, minBreakoutBarSizeTicks, requireCloseAbovePreHigh, enableWaveSizeOverride, waveSizeOverrideTicks, paintBars, showSwingHighLine, showSwingLowMarker, showVwap, stdDev1Multiplier, stdDev2Multiplier, vwapColor, stdDev1BandColor, stdDev2BandColor, vwapLineWidth, showCwap, cwapColor, cwapStdDev1BandColor, cwapStdDev2BandColor, cwapLineWidth, offsetTicksVwap, showVwapBreakMarker, enableValueAreaExit, barsInValueAreaForExit, showBarreBreakSignal, barreBreakTargetTicks, barreBreakStopLossTicks, barreBreakMarkerColor, barreBreakTextColor, showFirstBuySignal, firstBuyOffsetTicks, firstBuyTargetTicks, firstBuyStopLossTicks, firstBuyMarkerColor, firstBuyTextColor, showFirstBreakoutDownUpSignal, requireCloseAboveStdDev1, firstBreakoutDownUpTargetTicks, firstBreakoutDownUpZoneOffsetTicks, firstBreakoutDownUpStopLossTicks, firstBreakoutDownUpMarkerColor, firstBreakoutDownUpTextColor, showFirstBreakoutDownUpStd2Signal, requireCloseAboveStdDev2, firstBreakoutDownUpStd2TargetTicks, firstBreakoutDownUpStd2StopLossTicks, firstBreakoutDownUpStd2MarkerColor, firstBreakoutDownUpStd2TextColor);
		}

		public aaaSBVCfbV50Codex aaaSBVCfbV50Codex(ISeries<double> input, int strength, int lookBackPeriod, int offsetTicks, bool enableMinimumDistance, int minimumSwingDistanceTicks, bool enableUpBarFilter, int maxDownBarsAllowed, bool enableBarCountFilter, int minBarsRequired, int maxBarsAllowed, bool showRejectedBreakouts, bool enableBreakoutBarFilter, int minBreakoutBarSizeTicks, bool requireCloseAbovePreHigh, bool enableWaveSizeOverride, int waveSizeOverrideTicks, bool paintBars, bool showSwingHighLine, bool showSwingLowMarker, bool showVwap, double stdDev1Multiplier, double stdDev2Multiplier, Brush vwapColor, Brush stdDev1BandColor, Brush stdDev2BandColor, int vwapLineWidth, bool showCwap, Brush cwapColor, Brush cwapStdDev1BandColor, Brush cwapStdDev2BandColor, int cwapLineWidth, int offsetTicksVwap, bool showVwapBreakMarker, bool enableValueAreaExit, int barsInValueAreaForExit, bool showBarreBreakSignal, int barreBreakTargetTicks, int barreBreakStopLossTicks, Brush barreBreakMarkerColor, Brush barreBreakTextColor, bool showFirstBuySignal, int firstBuyOffsetTicks, int firstBuyTargetTicks, int firstBuyStopLossTicks, Brush firstBuyMarkerColor, Brush firstBuyTextColor, bool showFirstBreakoutDownUpSignal, bool requireCloseAboveStdDev1, int firstBreakoutDownUpTargetTicks, int firstBreakoutDownUpZoneOffsetTicks, int firstBreakoutDownUpStopLossTicks, Brush firstBreakoutDownUpMarkerColor, Brush firstBreakoutDownUpTextColor, bool showFirstBreakoutDownUpStd2Signal, bool requireCloseAboveStdDev2, int firstBreakoutDownUpStd2TargetTicks, int firstBreakoutDownUpStd2StopLossTicks, Brush firstBreakoutDownUpStd2MarkerColor, Brush firstBreakoutDownUpStd2TextColor)
		{
			if (cacheaaaSBVCfbV50Codex != null)
				for (int idx = 0; idx < cacheaaaSBVCfbV50Codex.Length; idx++)
					if (cacheaaaSBVCfbV50Codex[idx] != null && cacheaaaSBVCfbV50Codex[idx].Strength == strength && cacheaaaSBVCfbV50Codex[idx].LookBackPeriod == lookBackPeriod && cacheaaaSBVCfbV50Codex[idx].OffsetTicks == offsetTicks && cacheaaaSBVCfbV50Codex[idx].EnableMinimumDistance == enableMinimumDistance && cacheaaaSBVCfbV50Codex[idx].MinimumSwingDistanceTicks == minimumSwingDistanceTicks && cacheaaaSBVCfbV50Codex[idx].EnableUpBarFilter == enableUpBarFilter && cacheaaaSBVCfbV50Codex[idx].MaxDownBarsAllowed == maxDownBarsAllowed && cacheaaaSBVCfbV50Codex[idx].EnableBarCountFilter == enableBarCountFilter && cacheaaaSBVCfbV50Codex[idx].MinBarsRequired == minBarsRequired && cacheaaaSBVCfbV50Codex[idx].MaxBarsAllowed == maxBarsAllowed && cacheaaaSBVCfbV50Codex[idx].ShowRejectedBreakouts == showRejectedBreakouts && cacheaaaSBVCfbV50Codex[idx].EnableBreakoutBarFilter == enableBreakoutBarFilter && cacheaaaSBVCfbV50Codex[idx].MinBreakoutBarSizeTicks == minBreakoutBarSizeTicks && cacheaaaSBVCfbV50Codex[idx].RequireCloseAbovePreHigh == requireCloseAbovePreHigh && cacheaaaSBVCfbV50Codex[idx].EnableWaveSizeOverride == enableWaveSizeOverride && cacheaaaSBVCfbV50Codex[idx].WaveSizeOverrideTicks == waveSizeOverrideTicks && cacheaaaSBVCfbV50Codex[idx].PaintBars == paintBars && cacheaaaSBVCfbV50Codex[idx].ShowSwingHighLine == showSwingHighLine && cacheaaaSBVCfbV50Codex[idx].ShowSwingLowMarker == showSwingLowMarker && cacheaaaSBVCfbV50Codex[idx].ShowVwap == showVwap && cacheaaaSBVCfbV50Codex[idx].StdDev1Multiplier == stdDev1Multiplier && cacheaaaSBVCfbV50Codex[idx].StdDev2Multiplier == stdDev2Multiplier && cacheaaaSBVCfbV50Codex[idx].VwapColor == vwapColor && cacheaaaSBVCfbV50Codex[idx].StdDev1BandColor == stdDev1BandColor && cacheaaaSBVCfbV50Codex[idx].StdDev2BandColor == stdDev2BandColor && cacheaaaSBVCfbV50Codex[idx].VwapLineWidth == vwapLineWidth && cacheaaaSBVCfbV50Codex[idx].ShowCwap == showCwap && cacheaaaSBVCfbV50Codex[idx].CwapColor == cwapColor && cacheaaaSBVCfbV50Codex[idx].CwapStdDev1BandColor == cwapStdDev1BandColor && cacheaaaSBVCfbV50Codex[idx].CwapStdDev2BandColor == cwapStdDev2BandColor && cacheaaaSBVCfbV50Codex[idx].CwapLineWidth == cwapLineWidth && cacheaaaSBVCfbV50Codex[idx].OffsetTicksVwap == offsetTicksVwap && cacheaaaSBVCfbV50Codex[idx].ShowVwapBreakMarker == showVwapBreakMarker && cacheaaaSBVCfbV50Codex[idx].EnableValueAreaExit == enableValueAreaExit && cacheaaaSBVCfbV50Codex[idx].BarsInValueAreaForExit == barsInValueAreaForExit && cacheaaaSBVCfbV50Codex[idx].ShowBarreBreakSignal == showBarreBreakSignal && cacheaaaSBVCfbV50Codex[idx].BarreBreakTargetTicks == barreBreakTargetTicks && cacheaaaSBVCfbV50Codex[idx].BarreBreakStopLossTicks == barreBreakStopLossTicks && cacheaaaSBVCfbV50Codex[idx].BarreBreakMarkerColor == barreBreakMarkerColor && cacheaaaSBVCfbV50Codex[idx].BarreBreakTextColor == barreBreakTextColor && cacheaaaSBVCfbV50Codex[idx].ShowFirstBuySignal == showFirstBuySignal && cacheaaaSBVCfbV50Codex[idx].FirstBuyOffsetTicks == firstBuyOffsetTicks && cacheaaaSBVCfbV50Codex[idx].FirstBuyTargetTicks == firstBuyTargetTicks && cacheaaaSBVCfbV50Codex[idx].FirstBuyStopLossTicks == firstBuyStopLossTicks && cacheaaaSBVCfbV50Codex[idx].FirstBuyMarkerColor == firstBuyMarkerColor && cacheaaaSBVCfbV50Codex[idx].FirstBuyTextColor == firstBuyTextColor && cacheaaaSBVCfbV50Codex[idx].ShowFirstBreakoutDownUpSignal == showFirstBreakoutDownUpSignal && cacheaaaSBVCfbV50Codex[idx].RequireCloseAboveStdDev1 == requireCloseAboveStdDev1 && cacheaaaSBVCfbV50Codex[idx].FirstBreakoutDownUpTargetTicks == firstBreakoutDownUpTargetTicks && cacheaaaSBVCfbV50Codex[idx].FirstBreakoutDownUpZoneOffsetTicks == firstBreakoutDownUpZoneOffsetTicks && cacheaaaSBVCfbV50Codex[idx].FirstBreakoutDownUpStopLossTicks == firstBreakoutDownUpStopLossTicks && cacheaaaSBVCfbV50Codex[idx].FirstBreakoutDownUpMarkerColor == firstBreakoutDownUpMarkerColor && cacheaaaSBVCfbV50Codex[idx].FirstBreakoutDownUpTextColor == firstBreakoutDownUpTextColor && cacheaaaSBVCfbV50Codex[idx].ShowFirstBreakoutDownUpStd2Signal == showFirstBreakoutDownUpStd2Signal && cacheaaaSBVCfbV50Codex[idx].RequireCloseAboveStdDev2 == requireCloseAboveStdDev2 && cacheaaaSBVCfbV50Codex[idx].FirstBreakoutDownUpStd2TargetTicks == firstBreakoutDownUpStd2TargetTicks && cacheaaaSBVCfbV50Codex[idx].FirstBreakoutDownUpStd2StopLossTicks == firstBreakoutDownUpStd2StopLossTicks && cacheaaaSBVCfbV50Codex[idx].FirstBreakoutDownUpStd2MarkerColor == firstBreakoutDownUpStd2MarkerColor && cacheaaaSBVCfbV50Codex[idx].FirstBreakoutDownUpStd2TextColor == firstBreakoutDownUpStd2TextColor && cacheaaaSBVCfbV50Codex[idx].EqualsInput(input))
						return cacheaaaSBVCfbV50Codex[idx];
			return CacheIndicator<aaaSBVCfbV50Codex>(new aaaSBVCfbV50Codex(){ Strength = strength, LookBackPeriod = lookBackPeriod, OffsetTicks = offsetTicks, EnableMinimumDistance = enableMinimumDistance, MinimumSwingDistanceTicks = minimumSwingDistanceTicks, EnableUpBarFilter = enableUpBarFilter, MaxDownBarsAllowed = maxDownBarsAllowed, EnableBarCountFilter = enableBarCountFilter, MinBarsRequired = minBarsRequired, MaxBarsAllowed = maxBarsAllowed, ShowRejectedBreakouts = showRejectedBreakouts, EnableBreakoutBarFilter = enableBreakoutBarFilter, MinBreakoutBarSizeTicks = minBreakoutBarSizeTicks, RequireCloseAbovePreHigh = requireCloseAbovePreHigh, EnableWaveSizeOverride = enableWaveSizeOverride, WaveSizeOverrideTicks = waveSizeOverrideTicks, PaintBars = paintBars, ShowSwingHighLine = showSwingHighLine, ShowSwingLowMarker = showSwingLowMarker, ShowVwap = showVwap, StdDev1Multiplier = stdDev1Multiplier, StdDev2Multiplier = stdDev2Multiplier, VwapColor = vwapColor, StdDev1BandColor = stdDev1BandColor, StdDev2BandColor = stdDev2BandColor, VwapLineWidth = vwapLineWidth, ShowCwap = showCwap, CwapColor = cwapColor, CwapStdDev1BandColor = cwapStdDev1BandColor, CwapStdDev2BandColor = cwapStdDev2BandColor, CwapLineWidth = cwapLineWidth, OffsetTicksVwap = offsetTicksVwap, ShowVwapBreakMarker = showVwapBreakMarker, EnableValueAreaExit = enableValueAreaExit, BarsInValueAreaForExit = barsInValueAreaForExit, ShowBarreBreakSignal = showBarreBreakSignal, BarreBreakTargetTicks = barreBreakTargetTicks, BarreBreakStopLossTicks = barreBreakStopLossTicks, BarreBreakMarkerColor = barreBreakMarkerColor, BarreBreakTextColor = barreBreakTextColor, ShowFirstBuySignal = showFirstBuySignal, FirstBuyOffsetTicks = firstBuyOffsetTicks, FirstBuyTargetTicks = firstBuyTargetTicks, FirstBuyStopLossTicks = firstBuyStopLossTicks, FirstBuyMarkerColor = firstBuyMarkerColor, FirstBuyTextColor = firstBuyTextColor, ShowFirstBreakoutDownUpSignal = showFirstBreakoutDownUpSignal, RequireCloseAboveStdDev1 = requireCloseAboveStdDev1, FirstBreakoutDownUpTargetTicks = firstBreakoutDownUpTargetTicks, FirstBreakoutDownUpZoneOffsetTicks = firstBreakoutDownUpZoneOffsetTicks, FirstBreakoutDownUpStopLossTicks = firstBreakoutDownUpStopLossTicks, FirstBreakoutDownUpMarkerColor = firstBreakoutDownUpMarkerColor, FirstBreakoutDownUpTextColor = firstBreakoutDownUpTextColor, ShowFirstBreakoutDownUpStd2Signal = showFirstBreakoutDownUpStd2Signal, RequireCloseAboveStdDev2 = requireCloseAboveStdDev2, FirstBreakoutDownUpStd2TargetTicks = firstBreakoutDownUpStd2TargetTicks, FirstBreakoutDownUpStd2StopLossTicks = firstBreakoutDownUpStd2StopLossTicks, FirstBreakoutDownUpStd2MarkerColor = firstBreakoutDownUpStd2MarkerColor, FirstBreakoutDownUpStd2TextColor = firstBreakoutDownUpStd2TextColor }, input, ref cacheaaaSBVCfbV50Codex);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.aaaSBVCfbV50Codex aaaSBVCfbV50Codex(int strength, int lookBackPeriod, int offsetTicks, bool enableMinimumDistance, int minimumSwingDistanceTicks, bool enableUpBarFilter, int maxDownBarsAllowed, bool enableBarCountFilter, int minBarsRequired, int maxBarsAllowed, bool showRejectedBreakouts, bool enableBreakoutBarFilter, int minBreakoutBarSizeTicks, bool requireCloseAbovePreHigh, bool enableWaveSizeOverride, int waveSizeOverrideTicks, bool paintBars, bool showSwingHighLine, bool showSwingLowMarker, bool showVwap, double stdDev1Multiplier, double stdDev2Multiplier, Brush vwapColor, Brush stdDev1BandColor, Brush stdDev2BandColor, int vwapLineWidth, bool showCwap, Brush cwapColor, Brush cwapStdDev1BandColor, Brush cwapStdDev2BandColor, int cwapLineWidth, int offsetTicksVwap, bool showVwapBreakMarker, bool enableValueAreaExit, int barsInValueAreaForExit, bool showBarreBreakSignal, int barreBreakTargetTicks, int barreBreakStopLossTicks, Brush barreBreakMarkerColor, Brush barreBreakTextColor, bool showFirstBuySignal, int firstBuyOffsetTicks, int firstBuyTargetTicks, int firstBuyStopLossTicks, Brush firstBuyMarkerColor, Brush firstBuyTextColor, bool showFirstBreakoutDownUpSignal, bool requireCloseAboveStdDev1, int firstBreakoutDownUpTargetTicks, int firstBreakoutDownUpZoneOffsetTicks, int firstBreakoutDownUpStopLossTicks, Brush firstBreakoutDownUpMarkerColor, Brush firstBreakoutDownUpTextColor, bool showFirstBreakoutDownUpStd2Signal, bool requireCloseAboveStdDev2, int firstBreakoutDownUpStd2TargetTicks, int firstBreakoutDownUpStd2StopLossTicks, Brush firstBreakoutDownUpStd2MarkerColor, Brush firstBreakoutDownUpStd2TextColor)
		{
			return indicator.aaaSBVCfbV50Codex(Input, strength, lookBackPeriod, offsetTicks, enableMinimumDistance, minimumSwingDistanceTicks, enableUpBarFilter, maxDownBarsAllowed, enableBarCountFilter, minBarsRequired, maxBarsAllowed, showRejectedBreakouts, enableBreakoutBarFilter, minBreakoutBarSizeTicks, requireCloseAbovePreHigh, enableWaveSizeOverride, waveSizeOverrideTicks, paintBars, showSwingHighLine, showSwingLowMarker, showVwap, stdDev1Multiplier, stdDev2Multiplier, vwapColor, stdDev1BandColor, stdDev2BandColor, vwapLineWidth, showCwap, cwapColor, cwapStdDev1BandColor, cwapStdDev2BandColor, cwapLineWidth, offsetTicksVwap, showVwapBreakMarker, enableValueAreaExit, barsInValueAreaForExit, showBarreBreakSignal, barreBreakTargetTicks, barreBreakStopLossTicks, barreBreakMarkerColor, barreBreakTextColor, showFirstBuySignal, firstBuyOffsetTicks, firstBuyTargetTicks, firstBuyStopLossTicks, firstBuyMarkerColor, firstBuyTextColor, showFirstBreakoutDownUpSignal, requireCloseAboveStdDev1, firstBreakoutDownUpTargetTicks, firstBreakoutDownUpZoneOffsetTicks, firstBreakoutDownUpStopLossTicks, firstBreakoutDownUpMarkerColor, firstBreakoutDownUpTextColor, showFirstBreakoutDownUpStd2Signal, requireCloseAboveStdDev2, firstBreakoutDownUpStd2TargetTicks, firstBreakoutDownUpStd2StopLossTicks, firstBreakoutDownUpStd2MarkerColor, firstBreakoutDownUpStd2TextColor);
		}

		public Indicators.aaaSBVCfbV50Codex aaaSBVCfbV50Codex(ISeries<double> input , int strength, int lookBackPeriod, int offsetTicks, bool enableMinimumDistance, int minimumSwingDistanceTicks, bool enableUpBarFilter, int maxDownBarsAllowed, bool enableBarCountFilter, int minBarsRequired, int maxBarsAllowed, bool showRejectedBreakouts, bool enableBreakoutBarFilter, int minBreakoutBarSizeTicks, bool requireCloseAbovePreHigh, bool enableWaveSizeOverride, int waveSizeOverrideTicks, bool paintBars, bool showSwingHighLine, bool showSwingLowMarker, bool showVwap, double stdDev1Multiplier, double stdDev2Multiplier, Brush vwapColor, Brush stdDev1BandColor, Brush stdDev2BandColor, int vwapLineWidth, bool showCwap, Brush cwapColor, Brush cwapStdDev1BandColor, Brush cwapStdDev2BandColor, int cwapLineWidth, int offsetTicksVwap, bool showVwapBreakMarker, bool enableValueAreaExit, int barsInValueAreaForExit, bool showBarreBreakSignal, int barreBreakTargetTicks, int barreBreakStopLossTicks, Brush barreBreakMarkerColor, Brush barreBreakTextColor, bool showFirstBuySignal, int firstBuyOffsetTicks, int firstBuyTargetTicks, int firstBuyStopLossTicks, Brush firstBuyMarkerColor, Brush firstBuyTextColor, bool showFirstBreakoutDownUpSignal, bool requireCloseAboveStdDev1, int firstBreakoutDownUpTargetTicks, int firstBreakoutDownUpZoneOffsetTicks, int firstBreakoutDownUpStopLossTicks, Brush firstBreakoutDownUpMarkerColor, Brush firstBreakoutDownUpTextColor, bool showFirstBreakoutDownUpStd2Signal, bool requireCloseAboveStdDev2, int firstBreakoutDownUpStd2TargetTicks, int firstBreakoutDownUpStd2StopLossTicks, Brush firstBreakoutDownUpStd2MarkerColor, Brush firstBreakoutDownUpStd2TextColor)
		{
			return indicator.aaaSBVCfbV50Codex(input, strength, lookBackPeriod, offsetTicks, enableMinimumDistance, minimumSwingDistanceTicks, enableUpBarFilter, maxDownBarsAllowed, enableBarCountFilter, minBarsRequired, maxBarsAllowed, showRejectedBreakouts, enableBreakoutBarFilter, minBreakoutBarSizeTicks, requireCloseAbovePreHigh, enableWaveSizeOverride, waveSizeOverrideTicks, paintBars, showSwingHighLine, showSwingLowMarker, showVwap, stdDev1Multiplier, stdDev2Multiplier, vwapColor, stdDev1BandColor, stdDev2BandColor, vwapLineWidth, showCwap, cwapColor, cwapStdDev1BandColor, cwapStdDev2BandColor, cwapLineWidth, offsetTicksVwap, showVwapBreakMarker, enableValueAreaExit, barsInValueAreaForExit, showBarreBreakSignal, barreBreakTargetTicks, barreBreakStopLossTicks, barreBreakMarkerColor, barreBreakTextColor, showFirstBuySignal, firstBuyOffsetTicks, firstBuyTargetTicks, firstBuyStopLossTicks, firstBuyMarkerColor, firstBuyTextColor, showFirstBreakoutDownUpSignal, requireCloseAboveStdDev1, firstBreakoutDownUpTargetTicks, firstBreakoutDownUpZoneOffsetTicks, firstBreakoutDownUpStopLossTicks, firstBreakoutDownUpMarkerColor, firstBreakoutDownUpTextColor, showFirstBreakoutDownUpStd2Signal, requireCloseAboveStdDev2, firstBreakoutDownUpStd2TargetTicks, firstBreakoutDownUpStd2StopLossTicks, firstBreakoutDownUpStd2MarkerColor, firstBreakoutDownUpStd2TextColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.aaaSBVCfbV50Codex aaaSBVCfbV50Codex(int strength, int lookBackPeriod, int offsetTicks, bool enableMinimumDistance, int minimumSwingDistanceTicks, bool enableUpBarFilter, int maxDownBarsAllowed, bool enableBarCountFilter, int minBarsRequired, int maxBarsAllowed, bool showRejectedBreakouts, bool enableBreakoutBarFilter, int minBreakoutBarSizeTicks, bool requireCloseAbovePreHigh, bool enableWaveSizeOverride, int waveSizeOverrideTicks, bool paintBars, bool showSwingHighLine, bool showSwingLowMarker, bool showVwap, double stdDev1Multiplier, double stdDev2Multiplier, Brush vwapColor, Brush stdDev1BandColor, Brush stdDev2BandColor, int vwapLineWidth, bool showCwap, Brush cwapColor, Brush cwapStdDev1BandColor, Brush cwapStdDev2BandColor, int cwapLineWidth, int offsetTicksVwap, bool showVwapBreakMarker, bool enableValueAreaExit, int barsInValueAreaForExit, bool showBarreBreakSignal, int barreBreakTargetTicks, int barreBreakStopLossTicks, Brush barreBreakMarkerColor, Brush barreBreakTextColor, bool showFirstBuySignal, int firstBuyOffsetTicks, int firstBuyTargetTicks, int firstBuyStopLossTicks, Brush firstBuyMarkerColor, Brush firstBuyTextColor, bool showFirstBreakoutDownUpSignal, bool requireCloseAboveStdDev1, int firstBreakoutDownUpTargetTicks, int firstBreakoutDownUpZoneOffsetTicks, int firstBreakoutDownUpStopLossTicks, Brush firstBreakoutDownUpMarkerColor, Brush firstBreakoutDownUpTextColor, bool showFirstBreakoutDownUpStd2Signal, bool requireCloseAboveStdDev2, int firstBreakoutDownUpStd2TargetTicks, int firstBreakoutDownUpStd2StopLossTicks, Brush firstBreakoutDownUpStd2MarkerColor, Brush firstBreakoutDownUpStd2TextColor)
		{
			return indicator.aaaSBVCfbV50Codex(Input, strength, lookBackPeriod, offsetTicks, enableMinimumDistance, minimumSwingDistanceTicks, enableUpBarFilter, maxDownBarsAllowed, enableBarCountFilter, minBarsRequired, maxBarsAllowed, showRejectedBreakouts, enableBreakoutBarFilter, minBreakoutBarSizeTicks, requireCloseAbovePreHigh, enableWaveSizeOverride, waveSizeOverrideTicks, paintBars, showSwingHighLine, showSwingLowMarker, showVwap, stdDev1Multiplier, stdDev2Multiplier, vwapColor, stdDev1BandColor, stdDev2BandColor, vwapLineWidth, showCwap, cwapColor, cwapStdDev1BandColor, cwapStdDev2BandColor, cwapLineWidth, offsetTicksVwap, showVwapBreakMarker, enableValueAreaExit, barsInValueAreaForExit, showBarreBreakSignal, barreBreakTargetTicks, barreBreakStopLossTicks, barreBreakMarkerColor, barreBreakTextColor, showFirstBuySignal, firstBuyOffsetTicks, firstBuyTargetTicks, firstBuyStopLossTicks, firstBuyMarkerColor, firstBuyTextColor, showFirstBreakoutDownUpSignal, requireCloseAboveStdDev1, firstBreakoutDownUpTargetTicks, firstBreakoutDownUpZoneOffsetTicks, firstBreakoutDownUpStopLossTicks, firstBreakoutDownUpMarkerColor, firstBreakoutDownUpTextColor, showFirstBreakoutDownUpStd2Signal, requireCloseAboveStdDev2, firstBreakoutDownUpStd2TargetTicks, firstBreakoutDownUpStd2StopLossTicks, firstBreakoutDownUpStd2MarkerColor, firstBreakoutDownUpStd2TextColor);
		}

		public Indicators.aaaSBVCfbV50Codex aaaSBVCfbV50Codex(ISeries<double> input , int strength, int lookBackPeriod, int offsetTicks, bool enableMinimumDistance, int minimumSwingDistanceTicks, bool enableUpBarFilter, int maxDownBarsAllowed, bool enableBarCountFilter, int minBarsRequired, int maxBarsAllowed, bool showRejectedBreakouts, bool enableBreakoutBarFilter, int minBreakoutBarSizeTicks, bool requireCloseAbovePreHigh, bool enableWaveSizeOverride, int waveSizeOverrideTicks, bool paintBars, bool showSwingHighLine, bool showSwingLowMarker, bool showVwap, double stdDev1Multiplier, double stdDev2Multiplier, Brush vwapColor, Brush stdDev1BandColor, Brush stdDev2BandColor, int vwapLineWidth, bool showCwap, Brush cwapColor, Brush cwapStdDev1BandColor, Brush cwapStdDev2BandColor, int cwapLineWidth, int offsetTicksVwap, bool showVwapBreakMarker, bool enableValueAreaExit, int barsInValueAreaForExit, bool showBarreBreakSignal, int barreBreakTargetTicks, int barreBreakStopLossTicks, Brush barreBreakMarkerColor, Brush barreBreakTextColor, bool showFirstBuySignal, int firstBuyOffsetTicks, int firstBuyTargetTicks, int firstBuyStopLossTicks, Brush firstBuyMarkerColor, Brush firstBuyTextColor, bool showFirstBreakoutDownUpSignal, bool requireCloseAboveStdDev1, int firstBreakoutDownUpTargetTicks, int firstBreakoutDownUpZoneOffsetTicks, int firstBreakoutDownUpStopLossTicks, Brush firstBreakoutDownUpMarkerColor, Brush firstBreakoutDownUpTextColor, bool showFirstBreakoutDownUpStd2Signal, bool requireCloseAboveStdDev2, int firstBreakoutDownUpStd2TargetTicks, int firstBreakoutDownUpStd2StopLossTicks, Brush firstBreakoutDownUpStd2MarkerColor, Brush firstBreakoutDownUpStd2TextColor)
		{
			return indicator.aaaSBVCfbV50Codex(input, strength, lookBackPeriod, offsetTicks, enableMinimumDistance, minimumSwingDistanceTicks, enableUpBarFilter, maxDownBarsAllowed, enableBarCountFilter, minBarsRequired, maxBarsAllowed, showRejectedBreakouts, enableBreakoutBarFilter, minBreakoutBarSizeTicks, requireCloseAbovePreHigh, enableWaveSizeOverride, waveSizeOverrideTicks, paintBars, showSwingHighLine, showSwingLowMarker, showVwap, stdDev1Multiplier, stdDev2Multiplier, vwapColor, stdDev1BandColor, stdDev2BandColor, vwapLineWidth, showCwap, cwapColor, cwapStdDev1BandColor, cwapStdDev2BandColor, cwapLineWidth, offsetTicksVwap, showVwapBreakMarker, enableValueAreaExit, barsInValueAreaForExit, showBarreBreakSignal, barreBreakTargetTicks, barreBreakStopLossTicks, barreBreakMarkerColor, barreBreakTextColor, showFirstBuySignal, firstBuyOffsetTicks, firstBuyTargetTicks, firstBuyStopLossTicks, firstBuyMarkerColor, firstBuyTextColor, showFirstBreakoutDownUpSignal, requireCloseAboveStdDev1, firstBreakoutDownUpTargetTicks, firstBreakoutDownUpZoneOffsetTicks, firstBreakoutDownUpStopLossTicks, firstBreakoutDownUpMarkerColor, firstBreakoutDownUpTextColor, showFirstBreakoutDownUpStd2Signal, requireCloseAboveStdDev2, firstBreakoutDownUpStd2TargetTicks, firstBreakoutDownUpStd2StopLossTicks, firstBreakoutDownUpStd2MarkerColor, firstBreakoutDownUpStd2TextColor);
		}
	}
}

#endregion
