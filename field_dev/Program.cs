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
            int alrm;

            public TProc()
            {
                gas_v = 0;
                pump = 0;
                steam_v = 0;
                water_lvl = 0;
                pressure = 0;
                torch = false;
                alrm = 0;
            }

            public void Tick(S7Client c)
            {
                byte[] buffer = new byte[24];
                byte[] buffer_out = new byte[10];

                c.DBRead(1, 0, 24, buffer);
                gas_v =  S7.GetRealAt(buffer, 0);
                pump = S7.GetRealAt(buffer, 4);
                steam_v = S7.GetRealAt(buffer, 8);
                water_lvl = S7.GetRealAt(buffer, 12);
                pressure = S7.GetRealAt(buffer, 16);
                alrm = S7.GetIntAt(buffer, 20);
                torch = S7.GetBitAt(buffer, 22, 0);

                if (alrm == 0)
                {
                    water_lvl += 0.02f * pump;
                    if (torch)
                    {
                        water_lvl -= 0.01f * gas_v;
                        pressure += 0.01f * gas_v;
                    }
                    pressure -= 0.03f * steam_v;

                    if (water_lvl < 0)
                    {
                        water_lvl = 0;
                        alrm = 3;
                    }
                    if (water_lvl > 1)
                    {
                        water_lvl = 1;
                        alrm = 4;
                    }
                    if (pressure < 0.3f) pressure = 0.3f;
                    if (pressure > 0.95f)
                    {
                        alrm = 1;
                    }
                    if (pressure > 1)
                    {
                        alrm = 2;
                    }

                    S7.SetRealAt(buffer_out, 0, water_lvl);
                    S7.SetRealAt(buffer_out, 4, pressure);
                    S7.SetIntAt(buffer_out, 8, (short)alrm);
                    c.DBWrite(1, 12, 10, buffer_out);
                }
            }

            public void Tick(ModbusClient c)
            {
                gas_v = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(0, 2), ModbusClient.RegisterOrder.HighLow);
                pump = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(2, 2), ModbusClient.RegisterOrder.HighLow);
                steam_v = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(4, 2), ModbusClient.RegisterOrder.HighLow);
                water_lvl = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(6, 2), ModbusClient.RegisterOrder.HighLow);
                pressure = ModbusClient.ConvertRegistersToFloat(c.ReadHoldingRegisters(8, 2), ModbusClient.RegisterOrder.HighLow);
                torch = c.ReadCoils(5, 1)[0];

                alrm = c.ReadHoldingRegisters(10, 1)[0];

                if (alrm == 0)
                {
                    water_lvl += 0.02f * pump;
                    if (torch)
                    {
                        water_lvl -= 0.01f * gas_v;
                        pressure += 0.01f * gas_v;
                    }
                    pressure -= 0.03f * steam_v;

                    if (water_lvl < 0)
                    {
                        water_lvl = 0;
                        alrm = 3;
                    }
                    if (water_lvl > 1)
                    {
                        water_lvl = 1;
                        alrm = 4;
                    }
                    if (pressure < 0.3f) pressure = 0.3f;
                    if (pressure > 0.95f)
                    {
                        alrm = 1;
                    }
                    if (pressure > 1)
                    {
                        alrm = 2;
                    }

                    c.WriteMultipleRegisters(6, ModbusClient.ConvertFloatToRegisters(water_lvl, ModbusClient.RegisterOrder.HighLow));
                    c.WriteMultipleRegisters(8, ModbusClient.ConvertFloatToRegisters(pressure, ModbusClient.RegisterOrder.HighLow));
                    c.WriteSingleRegister(10, alrm);
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
                s7_client = new S7Client();
                try
                {
                    s7_client.ConnectTo("192.168.56.101", 0, 2);
                }
                catch
                {
                    Console.WriteLine("Cannot connect via S7 Comm, aborting execution");
                    return;
                }
                Console.WriteLine("Succesfully connected via S7 Comm, starting ticking...");
                while(true)
                {
                    Thread.Sleep(1000);
                    tp.Tick(s7_client);
                }
            }
            else
            {
                Console.WriteLine("Invalid mode of operation");
            }
        }

    }
}
