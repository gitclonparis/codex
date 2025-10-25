// Copyright (c) 2024 NinjaTrader, LLC. All rights reserved.
// NinjaTrader is a registered trademark of NinjaTrader, LLC.
//
// This script is subject to the terms of the NinjaTrader Public License.
// https://ninjatrader.com/NinjaTrader-Public-License-Agreement.pdf
//
// This script is provided "as is" without warranty of any kind. NinjaTrader, LLC. assumes no responsibility for the use or misuse of this script.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.OrderFlow;

namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// The deltabardiv_V2 indicator identifies divergences between the direction of a bar and its cumulative delta.
    /// It plots an up signal for up bars with negative delta and a down signal for down bars with positive delta.
    /// These signals are exposed as numerical values (1 for up signal, 2 for down signal) for use in other indicators or strategies.
    /// </summary>
    public class deltabardiv_V2 : Indicator
    {
        private OrderFlowCumulativeDelta orderFlowCumulativeDelta;

        // Series to hold the signal values for external access
        private Series<double> upSignals;
        private Series<double> downSignals;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = @"Identifies divergences between bar direction and cumulative delta (V2).";
                Name                        = "deltabardiv_V2";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = true;
                DisplayInDataBox            = true;
                DrawOnPricePanel            = true;
                IsSuspendedWhileInactive    = true;

                // Default values for the signals. These can be changed in the indicator settings.
                UpSignalValue = 1;
                DownSignalValue = 2;

                // Add plots for the visual signals on the chart.
                AddPlot(new Stroke(Brushes.Green, 2), PlotStyle.TriangleUp, "Up Divergence");
                AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.TriangleDown, "Down Divergence");

                // Add plots for the numerical signal values for the Data Box.
                // These are transparent so they don't appear on the chart.
                AddPlot(new Stroke(Brushes.Transparent), PlotStyle.Line, "Up Signal");
                AddPlot(new Stroke(Brushes.Transparent), PlotStyle.Line, "Down Signal");
            }
            else if (State == State.Configure)
            {
                // Add a tick-based data series to ensure accurate order flow data.
                AddDataSeries(Data.BarsPeriodType.Tick, 1);
            }
            else if (State == State.DataLoaded)
            {
                // Initialize the OrderFlowCumulativeDelta indicator.
                // The CumulativeDeltaPeriod is set to 'Bar' as requested.
                orderFlowCumulativeDelta = OrderFlowCumulativeDelta(Bars, CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0);

                // Initialize the series for signal values
                upSignals = new Series<double>(this);
                downSignals = new Series<double>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            // Process only the primary bars series
            if (BarsInProgress != 0) return;

            // Ensure we have enough data to work with
            if (CurrentBar < 1) return;

            // Check if the order flow indicator is initialized
            if (orderFlowCumulativeDelta == null) return;

            // Initialize plot values
            Values[0][0] = double.NaN; // Up Divergence (visual)
            Values[1][0] = double.NaN; // Down Divergence (visual)
            Values[2][0] = 0;          // Up Signal (numerical)
            Values[3][0] = 0;          // Down Signal (numerical)

            // Determine if the current bar is an up or down bar
            bool isUpBar = Close[0] > Open[0];
            bool isDownBar = Close[0] < Open[0];

            // Get the cumulative delta for the current bar.
            // The 'Delta' property of the OrderFlowCumulativeDelta indicator provides the delta for the current bar.
            double delta = orderFlowCumulativeDelta.Delta[0];

            // Initialize signal values for the current bar
            double upSignal = 0;
            double downSignal = 0;

            // Check for a divergence: an up bar with negative delta
            if (isUpBar && delta < 0)
            {
                upSignal = UpSignalValue;
                Values[0][0] = Low[0] - TickSize; // Position for the visual triangle
                Values[2][0] = upSignal;          // Value for the Data Box
            }
            // Check for a divergence: a down bar with positive delta
            else if (isDownBar && delta > 0)
            {
                downSignal = DownSignalValue;
                Values[1][0] = High[0] + TickSize; // Position for the visual triangle
                Values[3][0] = downSignal;           // Value for the Data Box
            }

            // Store the signal values in the series for external access
            upSignals[0] = upSignal;
            downSignals[0] = downSignal;
        }

        #region Properties
        /// <summary>
        /// Gets the series of up signals. Returns UpSignalValue (default 1) when an up bar has negative delta.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> UpSignals
        {
            get { return upSignals; }
        }

        /// <summary>
        /// Gets the series of down signals. Returns DownSignalValue (default 2) when a down bar has positive delta.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DownSignals
        {
            get { return downSignals; }
        }

        /// <summary>
        /// The numerical value to be used for an up signal (up bar with negative delta).
        /// </summary>
        [NinjaScriptProperty]
        [Display(Name = "Up Signal Value", Order = 1, GroupName = "Parameters", Description = "Value for up bar with negative delta")]
        public double UpSignalValue { get; set; }

        /// <summary>
        /// The numerical value to be used for a down signal (down bar with positive delta).
        /// </summary>
        [NinjaScriptProperty]
        [Display(Name = "Down Signal Value", Order = 2, GroupName = "Parameters", Description = "Value for down bar with positive delta")]
        public double DownSignalValue { get; set; }

        /// <summary>
        /// Gets the up divergence plot values. Returns triangle position when divergence is detected.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> UpDivergence
        {
            get { return Values[0]; }
        }

        /// <summary>
        /// Gets the down divergence plot values. Returns triangle position when divergence is detected.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DownDivergence
        {
            get { return Values[1]; }
        }

        /// <summary>
        /// Gets the up signal plot values for Data Box display.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> UpSignalPlot
        {
            get { return Values[2]; }
        }

        /// <summary>
        /// Gets the down signal plot values for Data Box display.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DownSignalPlot
        {
            get { return Values[3]; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private deltabardiv_V2[] cachedeltabardiv_V2;

		public deltabardiv_V2 deltabardiv_V2(double upSignalValue, double downSignalValue)
		{
			return deltabardiv_V2(Input, upSignalValue, downSignalValue);
		}

		public deltabardiv_V2 deltabardiv_V2(ISeries<double> input, double upSignalValue, double downSignalValue)
		{
			if (cachedeltabardiv_V2 != null)
				for (int idx = 0; idx < cachedeltabardiv_V2.Length; idx++)
					if (cachedeltabardiv_V2[idx] != null && cachedeltabardiv_V2[idx].UpSignalValue == upSignalValue && cachedeltabardiv_V2[idx].DownSignalValue == downSignalValue && cachedeltabardiv_V2[idx].EqualsInput(input))
						return cachedeltabardiv_V2[idx];
			return CacheIndicator<deltabardiv_V2>(new deltabardiv_V2(){ UpSignalValue = upSignalValue, DownSignalValue = downSignalValue }, input, ref cachedeltabardiv_V2);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		private deltabardiv_V2[] cachedeltabardiv_V2;

		public deltabardiv_V2 deltabardiv_V2(double upSignalValue, double downSignalValue)
		{
			return deltabardiv_V2(Input, upSignalValue, downSignalValue);
		}

		public deltabardiv_V2 deltabardiv_V2(ISeries<double> input, double upSignalValue, double downSignalValue)
		{
			if (cachedeltabardiv_V2 != null)
				for (int idx = 0; idx < cachedeltabardiv_V2.Length; idx++)
					if (cachedeltabardiv_V2[idx] != null && cachedeltabardiv_V2[idx].UpSignalValue == upSignalValue && cachedeltabardiv_V2[idx].DownSignalValue == downSignalValue && cachedeltabardiv_V2[idx].EqualsInput(input))
						return cachedeltabardiv_V2[idx];
			return CacheIndicator<deltabardiv_V2>(new deltabardiv_V2(){ UpSignalValue = upSignalValue, DownSignalValue = downSignalValue }, input, ref cachedeltabardiv_V2);
		}
	}
}

#endregion
