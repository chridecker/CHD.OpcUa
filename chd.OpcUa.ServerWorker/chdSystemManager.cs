using chd.OpcUa.Server.Interfaces;
using chd.OpcUa.Server.UnderlyingSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Opc.Ua;
using Opc.Ua.Server;

namespace chd.OpcUa.Server
{
    public class chdSystemManager : UnderlyingSystemManager
    {
        protected override ValueTask<List<UnderlyingSystemSegment>> LoadSegments(CancellationToken cancellationToken)
            => ValueTask.FromResult(Build());

        private List<UnderlyingSystemSegment> Build()
        {
            var factory = CreateChild("Factory", null);
            var assets = CreateChild("Assets", null);
            var testData = CreateChild("TestData", null);

            var east = CreateChild("East", factory);
            var west = CreateChild("West", factory);

            var boiler1 = CreateChild("Boiler1", east);
            var boiler2 = CreateChild("Boiler2", west);

            boiler1.Blocks.AddRange(["Pipe1001", "Pipe1002", "Drum1002", "FC1001", "LC1001", "CC1001"]);
            boiler2.Blocks.AddRange(["Pipe2001", "Pipe2002", "Drum2002", "FC2001", "LC2001", "CC2001"]);

            east.Children.Add(boiler1);
            west.Children.Add(boiler2);
            factory.Children.AddRange([east, west]);

            var sensors = CreateChild("Sensors", assets);
            var sensorFlow = CreateChild("Flow", sensors);
            sensorFlow.Blocks.AddRange(["Pipe2001", "Pipe1002", "Pipe2001", "Pipe2002"]);

            var sensorLevel = CreateChild("Level", sensors);
            sensorLevel.Blocks.AddRange(["Drum1002", "Drum2002"]);
            sensors.Children.AddRange([sensorLevel, sensorFlow]);

            var controllers = CreateChild("Controllers", assets);
            var controllersFlow = CreateChild("Flow", sensors);
            controllersFlow.Blocks.AddRange(["FC1001", "FC2001"]);
            var controllersLevel = CreateChild("Level", sensors);
            controllersLevel.Blocks.AddRange(["LC1001", "LC2001"]);
            var controllersCustom = CreateChild("Custom", sensors);
            controllersCustom.Blocks.AddRange(["CC1001", "CC2001"]);
            controllers.Children.AddRange([controllersFlow, controllersLevel, controllersCustom]);

            assets.Children.AddRange([sensors, controllers]);

            return [factory, assets, testData];
        }

        protected override async ValueTask<UnderlyingSystemBlock> CreateBlockAsync(string blockName, CancellationToken cancellationToken)
        {
            var block = new UnderlyingSystemBlock(blockName, GetBockType(blockName));
            HandleBlock(block);
            return block;
        }

        private UnderlyingSystemSegment CreateChild(string name, UnderlyingSystemSegment parent)
            => new(name, parent);

        private string GetBockType(string blockName) => blockName switch
        {
            var x when x.StartsWith("Pipe") => "FlowSensor",
            var x when x.StartsWith("Drum") => "LevelSensor",
            var x when x.StartsWith("FC") || x.StartsWith("LC") => "Controller",
            var x when x.StartsWith("CC") => "CustomController",
            _ => "Unknown"
        };


        private void HandleBlock(UnderlyingSystemBlock block)
        {
            switch (block.BlockType)
            {
                case "FlowSensor":
                    {
                        block.CreateTag("Measurement", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Analog,
                            "liters/sec", false);
                        block.CreateTag("Online", UnderlyingSystemDataType.Integer1, UnderlyingSystemTagType.Digital, null,
                            false);
                        break;
                    }

                case "LevelSensor":
                    {
                        block.CreateTag("Measurement", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Analog,
                            "liters", false);
                        block.CreateTag("Online", UnderlyingSystemDataType.Integer1, UnderlyingSystemTagType.Digital, null,
                            false);
                        break;
                    }

                case "Controller":
                    {
                        block.CreateTag("SetPoint", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null,
                            true);
                        block.CreateTag("Measurement", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null,
                            false);
                        block.CreateTag("Output", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null,
                            false);
                        block.CreateTag("Status", UnderlyingSystemDataType.Integer4, UnderlyingSystemTagType.Enumerated,
                            null, false);
                        break;
                    }

                case "CustomController":
                    {
                        block.CreateTag("Input1", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null,
                            true);
                        block.CreateTag("Input2", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null,
                            true);
                        block.CreateTag("Input3", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null,
                            true);
                        block.CreateTag("Output", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null,
                            false);

                        block.CreateMethod(nameof(StartCustomController), HandleCustomController);
                        block.MethodExecution += Block_MethodExecution;
                        break;
                    }
            }
        }

        private async ValueTask<object[]> Block_MethodExecution(UnderlyingSystemMethod method, object[] inputs, CancellationToken cancellationToken)
        {
            var execution = this.GetType().GetMethod(method.Name);
            if (execution is null)
            {
                throw new NotImplementedException($"Konnte die Methode {method.Name} nicht finden!");
            }

            var result = execution.Invoke(this, [inputs, cancellationToken]);
            if (execution.ReturnType == typeof(ValueTask<object[]>))
            {
                return await (ValueTask<object[]>)result;
            }
            if (execution.ReturnType == typeof(ValueTask))
            {
                await (ValueTask)result;
            }
            if (execution.ReturnType == typeof(object[]))
            {
                return (object[])result;
            }
            return Array.Empty<object>();
        }

        public ValueTask<object[]> StartCustomController(object[] inputs, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(inputs);
        }

        private static void HandleCustomController(UnderlyingSystemMethod method)
        {
            method.CreateInputArgument("Initial State", typeof(uint));
            method.CreateInputArgument("Final State", typeof(uint));
            method.CreateOutputArgument("Initial State", typeof(uint));
            method.CreateOutputArgument("Final State", typeof(uint));
        }
    }
}
