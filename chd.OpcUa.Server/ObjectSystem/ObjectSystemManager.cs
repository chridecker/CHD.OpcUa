using chd.OpcUa.Base.Extensions;
using chd.OpcUa.Contracts.Interfaces;
using chd.OpcUa.Server.UnderlyingSystem;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace chd.OpcUa.Server.ObjectSystem
{
    public abstract class ObjectSystemManager : UnderlyingSystemManager
    {
        private ConcurrentDictionary<string, List<IUaServerObject>> _objectStore = [];
        protected abstract List<IUaServerObject> GetObjects();

        protected override ValueTask<List<UnderlyingSystemSegment>> LoadSegments(CancellationToken cancellationToken)
        {
            var returnLst = new List<UnderlyingSystemSegment>();
            var pluginsSegment = new UnderlyingSystemSegment("Plugins");
            var lst = GetObjects();
            foreach (var group in lst.GroupBy(s => s.GetType()))
            {
                _objectStore[group.Key.Name] = group.ToList();
                var plugin = new UnderlyingSystemSegment(group.Key.Name, pluginsSegment);
                plugin.Blocks.AddRange(group.ToList().Select(s => s.Name).ToArrayOf());
                pluginsSegment.Children.Add(plugin);
            }
            returnLst.Add(pluginsSegment);
            return ValueTask.FromResult(returnLst);
        }

        protected override async ValueTask<UnderlyingSystemBlock> CreateBlockAsync(string blockName, CancellationToken cancellationToken)
        {
            UnderlyingSystemBlock block = null;
            var entry = _objectStore.FirstOrDefault(x => x.Value.Any(a => a.Name == blockName));
            if (entry.Value.Any())
            {
                var instance = entry.Value.FirstOrDefault(x => x.Name == blockName);
                block = new UnderlyingSystemBlock(blockName, instance.Description, entry.Key);
                await HandleSystemObject(block, instance);
            }
            return block;
        }

        protected override (MethodInfo? method, object instance) GetMethodInfo(UnderlyingSystemMethod method)
        {
            var entry = _objectStore.FirstOrDefault(x => x.Value.Any(a => a.Name == method.Block.Name));

            var instance = entry.Value.FirstOrDefault(x => x.Name == method.Block.Name);

            var methodInfo = instance?.GetType().GetMethod(method.Name) ??
            instance.GetType().GetMethods()
                .Where(x => x.IsDefined(typeof(ObjectSystemMethodAttribute), inherit: true))
                .FirstOrDefault(x => x.GetCustomAttribute<ObjectSystemMethodAttribute>().DisplayName == method.Name);
            return (methodInfo, instance);
        }

        private async ValueTask HandleSystemObject(UnderlyingSystemBlock block, IUaServerObject instance)
        {
            var realType = instance.GetType();
            foreach (var prop in realType.GetProperties().Where(x => x.IsDefined(typeof(ObjectSystemPropertyAttribute), inherit: true)))
            {
                var attribute = prop.GetCustomAttribute<ObjectSystemPropertyAttribute>();
                block.CreateTag(prop.PropertyType, attribute?.DisplayName ?? prop.Name, string.Empty, attribute.CanWrite, prop.PropertyType.IsEnum ? Enum.GetNames(prop.PropertyType) : null,
                    (ct) => ReadValueAsync(instance, prop, ct),
                    (value, ct) => WriteValueAsync(instance, prop, value, ct)
                    );
            }

            foreach (var method in realType.GetMethods().Where(m => m.IsDefined(typeof(ObjectSystemMethodAttribute), inherit: true)))
            {
                var attribute = method.GetCustomAttribute<ObjectSystemMethodAttribute>();

                block.AddMethod(attribute?.DisplayName ?? method.Name, attribute?.Description ?? string.Empty, attribute.CanExecute, HandleSystemMethod);
            }

            if (instance is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += NotifyPropertyChanged_PropertyChanged;
            }

        }

        private async void NotifyPropertyChanged_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is IUaServerObject instance)
            {
                var block = await this.FindBlockByIdentifier(instance.Name, CancellationToken.None);
                if (block.GetTags().Any(a => a.Name == e.PropertyName))
                {
                    block.TagChanged(e.PropertyName);
                }
                else
                {
                    var propInfo = instance.GetType().GetProperty(e.PropertyName);
                    block.TagChanged(propInfo.GetCustomAttribute<ObjectSystemPropertyAttribute>().DisplayName);
                }



            }
        }

        private void HandleSystemMethod(UnderlyingSystemMethod method)
        {
            var (methodInfo, _) = GetMethodInfo(method);
            if (methodInfo is null)
            {
                return;
            }

            foreach (var inputArg in methodInfo.GetParameters().Where(x => x.ParameterType != typeof(CancellationToken)))
            {
                method.CreateInputArgument(inputArg.Name, inputArg.ParameterType);
            }

            if (method.OutputArguments.Count == 0
                && methodInfo.ReturnType.GenericTypeArguments.Any())
            {
                method.CreateOutputArgument("Result", methodInfo.ReturnType.GenericTypeArguments[0]);
            }
            else if (method.OutputArguments.Count == 0
                     && methodInfo.ReturnType != typeof(void))
            {
                method.CreateOutputArgument("Result", methodInfo.ReturnType);
            }
        }

        private ValueTask<Variant> ReadValueAsync(IUaServerObject instance, PropertyInfo propInfo, CancellationToken cancellationToken)
        {
            var val = propInfo.GetValue(instance);
            if (propInfo.PropertyType.IsEnum)
            {
                return ValueTask.FromResult(Enum.GetName(propInfo.PropertyType, val).ConvertToVariant());
            }
            return ValueTask.FromResult(val.ConvertToVariant());
        }

        private ValueTask WriteValueAsync(IUaServerObject instance, PropertyInfo propInfo, Variant value, CancellationToken cancellationToken)
        {
            propInfo.SetValue(instance, value.GetValue());
            return ValueTask.CompletedTask;
        }
    }
}
