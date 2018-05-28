using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Sharp7;
using EasyModbus;

namespace field_dev
{
    class Program
    {

        class TProc
        {
            float gas_v;
            float pump;
            float steam_v;
            float water_lvl;
            float pressure;
            bool torch;
            bool alrm;

            public TProc()
            {
                gas_v = 0;
                pump = 0;
                steam_v = 0;
                water_lvl = 0;
                pressure = 0;
                torch = false;
                alrm = false;
            }

            public void Tick(S7Client c)
            {

            }

            public void Tick(ModbusClient c)
            {
                gas_v = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(0, 2), ModbusClient.RegisterOrder.HighLow);
                pump = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(2, 2), ModbusClient.RegisterOrder.HighLow);
                steam_v = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(4, 2), ModbusClient.RegisterOrder.HighLow);
                water_lvl = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(6, 2), ModbusClient.RegisterOrder.HighLow);
                pressure = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(8, 2), ModbusClient.RegisterOrder.HighLow);
                torch = c.ReadCoils(5, 1)[0];
                alrm = c.ReadCoils(4, 1)[0];

                if (!alrm)
                {
                    water_lvl += 0.02f * pump;
                    if (torch)
                    {
                        water_lvl -= 0.01f * gas_v;
                        pressure += 0.01f * gas_v;
                    }
                    pressure -= 0.03f * steam_v;

                    if (water_lvl < 0) water_lvl = 0;
                    if (water_lvl > 1)
                    {
                        water_lvl = 1;
                        alrm = true;
                    }
                    if (pressure < 0.3f) pressure = 0.3f;
                    if (pressure > 1)
                    {
                        alrm = true;
                    }

                    c.WriteMultipleRegisters(6, ModbusClient.ConvertFloatToRegisters(water_lvl, ModbusClient.RegisterOrder.HighLow));
                    c.WriteMultipleRegisters(8, ModbusClient.ConvertFloatToRegisters(pressure, ModbusClient.RegisterOrder.HighLow));
                    c.WriteSingleCoil(4, alrm);

                }
                else
                {

                }
            }
        }

        static void Main(string[] args)
        {
            ModbusClient mbus_client;
            S7Client s7_client;
            TProc tp = new TProc();
            string ans;

            Console.WriteLine("Select mode of operation:\n(M)odbus, (S)7Comm");
            ans = Console.ReadLine();
            if (ans == "M" || ans == "m")
            {
                mbus_client = new ModbusClient("127.0.0.1", 502);
                try
                {
                    mbus_client.Connect();
                }
                catch
                {
                    Console.WriteLine("Cannot connect via Modbus TCP, aborting execution");
                    return;
                }
                Console.WriteLine("Succesfully connected via Modbus TCP, starting ticking...");
                while (true)
                {
                    Thread.Sleep(1000);
                    tp.Tick(mbus_client);
                }
            }
            else if (ans == "S" || ans == "s")
            {

            }
            else
            {
                Console.WriteLine("Invalid mode of operation");
            }
        }

    }
}
