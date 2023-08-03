using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using CommonModule.Common.Core;
using CommonModule.Core;

namespace CanBusParameterSetting
{
    /* 
     * hz 정보
     * 1 mhz = 1,000 khz = 1,000,000 hz
     */

    public class BusSettingInfo
    {
        public int Bitrate { get; set; }
        public int Tseg1 { get; set; }
        public int Tseg2 { get; set; }
        public int? Sjw { get; set; }
        public double? Samplepoint { get; set; }
        public double? Prescaler { get; set; }
    }

    public class DetailBusSetting
    {
        public double Samplepoint { get; set; }
        public int BtlCycle { get; set; }
        public int Tseg1 { get; set; }
        public int Tseg2 { get; set; }
        public double? Prescaler { get; set; }
    }

    public class CanBitTimingCalculator : Singleton<CanBitTimingCalculator>
    {
        public new static CanBitTimingCalculator Instance
        {
            get
            {
                Initializer(() => { return new CanBitTimingCalculator(); });
                return Singleton<CanBitTimingCalculator>.Instance;
            }
        }

        //prescaler (prescaler in only int type)
        public decimal Prescaler(int? bitrate, int? tseg1, int? tseg2, int hz = 8000000)
        {
            if (bitrate != null && tseg1 != null && tseg2 != null)
                return (decimal)(bitrate.Value * (1 + tseg1.Value + tseg2.Value)) / (hz / 1000);
            return 0;
        }

        //sample point 
        public double? SamplePoint(int? tseg1, int? tseg2)
        {
            if (tseg1 != null && tseg2 != null)
                return 100.0 * (1 + tseg1.Value) / (1 + tseg1.Value + tseg2.Value);
            return null;
        }

        //Number of Tq 
        // * Max BTL => totalTq x 10
        //public double NumberOfTimeQuanta(double bitrate, int hz)
        //{
        //    int mhz = hz / 1000000;
        //    double tq = 1000 / mhz;
        //    double bitTime = 1000 / bitrate;
        //    double totalTq = (bitTime * 1000) / tq;

        //    return totalTq;
        //}

        public BusSettingInfo AddBusParamsInfo(int bitrate, int tseg1, int tseg2, int sjw)
        {
            BusSettingInfo setting = new();
            setting.Bitrate = bitrate;
            setting.Tseg1 = tseg1;
            setting.Tseg2 = tseg2;
            setting.Sjw = sjw;
            setting.Samplepoint = SamplePoint(tseg1, tseg2);
            setting.Prescaler = (int)Prescaler(bitrate, tseg1, tseg2);
            return setting;
        }

        //CAN(FD) Bitrate에 따른 Prescaler/SamplePoint/Tseg1/Tseg2 계산
        public List<DetailBusSetting> DetailData(int bitrate, int khz)
        {
            int prescaler = 0;
            int btl;
            List<DetailBusSetting> detailBusSetting = new();

            while (true)
            {
                prescaler++;
                btl = (int)((khz / bitrate) / prescaler);

                for (int tseg1 = 1; tseg1 <= btl; tseg1++)
                {
                    for (int tseg2 = 1; tseg2 <= btl - tseg1; tseg2++)
                    {
                        if (khz == bitrate * ((1 + tseg1 + tseg2)) * prescaler && btl == tseg1 + tseg2 + 1 && Math.Round((double)SamplePoint(tseg1, tseg2), 1, MidpointRounding.AwayFromZero) >= 50)
                        {
                            DetailBusSetting setting = new();
                            setting.Samplepoint = Math.Round((double)SamplePoint(tseg1, tseg2), 1, MidpointRounding.AwayFromZero);
                            setting.BtlCycle = btl;
                            setting.Tseg1 = tseg1;
                            setting.Tseg2 = tseg2;
                            setting.Prescaler = prescaler;
                            detailBusSetting.Add(setting);
                        }
                    }
                }
                if (prescaler * bitrate >= khz)
                {
                    break;
                }
            }
            return detailBusSetting;
        }
    }
}
