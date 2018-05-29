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
                byte[] dw_buffer = new byte[4];
                byte[] w_buffer = new byte[2];
                c.DBRead(1, 0, 4, dw_buffer);
                gas_v = BitConverter.ToSingle(dw_buffer, 0);
                c.DBRead(1, 4, 4, dw_buffer);
                pump = BitConverter.ToSingle(dw_buffer, 0);
                c.DBRead(1, 8, 4, dw_buffer);
                steam_v = BitConverter.ToSingle(dw_buffer, 0);
                c.DBRead(1, 12, 4, dw_buffer);
                water_lvl = BitConverter.ToSingle(dw_buffer, 0);
                c.DBRead(1, 16, 4, dw_buffer);
                pressure = BitConverter.ToSingle(dw_buffer, 0);
                c.DBRead(1, 20, 2, w_buffer);
                alrm = BitConverter.ToInt16(w_buffer, 0);
                c.DBRead(1, 22, 2, w_buffer);
                torch = BitConverter.ToBoolean(w_buffer, 1);

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

                    c.DBWrite(1, 12, 4, BitConverter.GetBytes(water_lvl));
                    c.DBWrite(1, 16, 4, BitConverter.GetBytes(pressure));
                    c.DBWrite(1, 20, 2, BitConverter.GetBytes((short)alrm));
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
                    s7_client.ConnectTo("127.0.0.1", 0, 2);
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
