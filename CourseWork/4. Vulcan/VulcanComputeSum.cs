using Silk.NET.Vulkan;
using Silk.NET.Core.Native;
using System.Runtime.InteropServices;
using SystemBuffer = System.Buffer;
using Buffer = Silk.NET.Vulkan.Buffer;

public class VulkanComputeSum
{
    public static unsafe void Main(string filePath)
    {
        var vk = Vk.GetApi();

        // ---------- Инициализация (instance, device, queue) ----------
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)SilkMarshal.StringToPtr("UniformShader"),
            ApplicationVersion = 1,
            PEngineName = (byte*)SilkMarshal.StringToPtr(""),
            EngineVersion = 1,
            ApiVersion = Vk.Version12
        };

        var instanceCreateInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };
        Instance instance;
        vk.CreateInstance(&instanceCreateInfo, null, &instance);

        uint deviceCount = 0;
        vk.EnumeratePhysicalDevices(instance, &deviceCount, null);
        var physicalDevices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* pDevices = physicalDevices)
            vk.EnumeratePhysicalDevices(instance, &deviceCount, pDevices);
        PhysicalDevice physicalDevice = physicalDevices[0];

        float queuePriority = 1.0f;
        var deviceQueueCreateInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = 0,
            QueueCount = 1,
            PQueuePriorities = &queuePriority
        };
        var deviceCreateInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &deviceQueueCreateInfo
        };
        Device device;
        vk.CreateDevice(physicalDevice, &deviceCreateInfo, null, &device);

        Queue queue;
        vk.GetDeviceQueue(device, 0, 0, &queue);

        // ---------- Загрузка SPIR‑V ----------
        byte[] spvBytes = File.ReadAllBytes(filePath);
        uint[] spvWords = new uint[spvBytes.Length / 4];
        SystemBuffer.BlockCopy(spvBytes, 0, spvWords, 0, spvBytes.Length);

        ShaderModule shaderModule;
        fixed (uint* pCode = spvWords)
        {
            var shaderCreateInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spvBytes.Length,
                PCode = pCode
            };
            vk.CreateShaderModule(device, &shaderCreateInfo, null, &shaderModule);
        }

        // ---------- Uniform‑буфер для двух float (a, b) ----------
        const int UniformSize = 2 * sizeof(float); // 8 байт
        float a = 3.5f;
        float b = 2.5f;
        byte[] uniformData = new byte[UniformSize];
        SystemBuffer.BlockCopy(BitConverter.GetBytes(a), 0, uniformData, 0, 4);
        SystemBuffer.BlockCopy(BitConverter.GetBytes(b), 0, uniformData, 4, 4);

        var uniformBufferCreateInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)UniformSize,
            Usage = BufferUsageFlags.UniformBufferBit | BufferUsageFlags.TransferDstBit,
            SharingMode = SharingMode.Exclusive
        };
        Buffer uniformBuffer;
        vk.CreateBuffer(device, &uniformBufferCreateInfo, null, &uniformBuffer);

        // Память для uniform-буфера
        MemoryRequirements memReqs;
        vk.GetBufferMemoryRequirements(device, uniformBuffer, &memReqs);
        PhysicalDeviceMemoryProperties memProps;
        vk.GetPhysicalDeviceMemoryProperties(physicalDevice, &memProps);
        uint memoryTypeIndex = uint.MaxValue;
        for (int i = 0; i < memProps.MemoryTypeCount; i++)
        {
            if ((memReqs.MemoryTypeBits & (1u << (int)i)) != 0 &&
                (memProps.MemoryTypes[i].PropertyFlags & (MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)) != 0)
            {
                memoryTypeIndex = (uint)i;
                break;
            }
        }
        var uniformAllocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = memoryTypeIndex
        };
        DeviceMemory uniformMemory;
        vk.AllocateMemory(device, &uniformAllocInfo, null, &uniformMemory);
        vk.BindBufferMemory(device, uniformBuffer, uniformMemory, 0);

        // Заполняем uniform-буфер данными
        void* mappedData;
        vk.MapMemory(device, uniformMemory, 0, (ulong)UniformSize, 0, &mappedData);
        Marshal.Copy(uniformData, 0, (nint)mappedData, UniformSize);
        vk.UnmapMemory(device, uniformMemory);

        // ---------- Storage‑буфер для результата (один float) ----------
        const int OutputSize = sizeof(float);
        var outputBufferCreateInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)OutputSize,
            Usage = BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive
        };
        Buffer outputBuffer;
        vk.CreateBuffer(device, &outputBufferCreateInfo, null, &outputBuffer);

        // Память для output-буфера
        vk.GetBufferMemoryRequirements(device, outputBuffer, &memReqs);
        memoryTypeIndex = uint.MaxValue;
        for (int i = 0; i < memProps.MemoryTypeCount; i++)
        {
            if ((memReqs.MemoryTypeBits & (1u << (int)i)) != 0 &&
                (memProps.MemoryTypes[i].PropertyFlags & (MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)) != 0)
            {
                memoryTypeIndex = (uint)i;
                break;
            }
        }
        var outputAllocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = memoryTypeIndex
        };
        DeviceMemory outputMemory;
        vk.AllocateMemory(device, &outputAllocInfo, null, &outputMemory);
        vk.BindBufferMemory(device, outputBuffer, outputMemory, 0);

        // ---------- Дескрипторы: set=0, binding=0 (Uniform), binding=1 (Storage) ----------
        // Layout: два биндинга
        var bindings = new DescriptorSetLayoutBinding[2];
        bindings[0] = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        bindings[1] = new DescriptorSetLayoutBinding
        {
            Binding = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.ComputeBit
        };

        DescriptorSetLayout descriptorSetLayout;
        fixed (DescriptorSetLayoutBinding* pBindings = bindings)
        {
            var layoutCreateInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindings.Length,
                PBindings = pBindings
            };
            vk.CreateDescriptorSetLayout(device, &layoutCreateInfo, null, &descriptorSetLayout);
        }

        var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &descriptorSetLayout
        };
        PipelineLayout pipelineLayout;
        vk.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, &pipelineLayout);

        // Дескрипторный пул с двумя типами
        var poolSizes = new DescriptorPoolSize[2];
        poolSizes[0] = new DescriptorPoolSize { Type = DescriptorType.UniformBuffer, DescriptorCount = 1 };
        poolSizes[1] = new DescriptorPoolSize { Type = DescriptorType.StorageBuffer, DescriptorCount = 1 };
        DescriptorPool descriptorPool;
        fixed (DescriptorPoolSize* pPoolSizes = poolSizes)
        {
            var poolCreateInfo = new DescriptorPoolCreateInfo
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                MaxSets = 1,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = pPoolSizes
            };
            vk.CreateDescriptorPool(device, &poolCreateInfo, null, &descriptorPool);
        }

        // Выделение набора
        var setAllocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &descriptorSetLayout
        };
        DescriptorSet descriptorSet;
        vk.AllocateDescriptorSets(device, &setAllocInfo, &descriptorSet);

        // Связываем буферы
        var bufferInfos = new DescriptorBufferInfo[2];
        bufferInfos[0] = new DescriptorBufferInfo { Buffer = uniformBuffer, Offset = 0, Range = (ulong)UniformSize };
        bufferInfos[1] = new DescriptorBufferInfo { Buffer = outputBuffer, Offset = 0, Range = (ulong)OutputSize };

        var writeDescriptors = new WriteDescriptorSet[2];

        // <-- обязательно закрепляем bufferInfos -->
        fixed (DescriptorBufferInfo* pBufferInfos = bufferInfos)
        {
            writeDescriptors[0] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = descriptorSet,
                DstBinding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                PBufferInfo = &pBufferInfos[0]
            };
            writeDescriptors[1] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = descriptorSet,
                DstBinding = 1,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageBuffer,
                PBufferInfo = &pBufferInfos[1]
            };
        }

        // Далее передаём writeDescriptors в UpdateDescriptorSets
        fixed (WriteDescriptorSet* pWrites = writeDescriptors)
        {
            vk.UpdateDescriptorSets(device, (uint)writeDescriptors.Length, pWrites, 0, null);
        }

        // ---------- Пайплайн ----------
        byte* entryPointName = (byte*)SilkMarshal.StringToPtr("main");
        var shaderStage = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            Module = shaderModule,
            PName = entryPointName
        };
        var computePipelineCreateInfo = new ComputePipelineCreateInfo
        {
            SType = StructureType.ComputePipelineCreateInfo,
            Stage = shaderStage,
            Layout = pipelineLayout
        };
        Pipeline pipeline;
        vk.CreateComputePipelines(device, default, 1, &computePipelineCreateInfo, null, &pipeline);

        // ---------- Командный буфер ----------
        var commandPoolCreateInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = 0
        };
        CommandPool commandPool;
        vk.CreateCommandPool(device, &commandPoolCreateInfo, null, &commandPool);

        var commandBufferAllocateInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };
        CommandBuffer commandBuffer;
        vk.AllocateCommandBuffers(device, &commandBufferAllocateInfo, &commandBuffer);

        var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };
        vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Compute, pipeline);
        vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Compute, pipelineLayout, 0, 1, &descriptorSet, 0, null);

        // Запускаем одну рабочую группу (local_size_x = 1)
        vk.CmdDispatch(commandBuffer, 1, 1, 1);

        // Барьер для когерентности перед чтением с хоста
        var memBarrier = new MemoryBarrier
        {
            SType = StructureType.MemoryBarrier,
            SrcAccessMask = AccessFlags.ShaderWriteBit,
            DstAccessMask = AccessFlags.HostReadBit
        };
        vk.CmdPipelineBarrier(commandBuffer,
            PipelineStageFlags.ComputeShaderBit,
            PipelineStageFlags.HostBit,
            DependencyFlags.None,
            1, &memBarrier,
            0, null,
            0, null);

        vk.EndCommandBuffer(commandBuffer);

        // ---------- Отправка и ожидание ----------
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };
        vk.QueueSubmit(queue, 1, &submitInfo, default);
        vk.QueueWaitIdle(queue);

        // ---------- Чтение результата ----------
        float result = 0;
        vk.MapMemory(device, outputMemory, 0, (ulong)OutputSize, 0, &mappedData);
        result = Marshal.PtrToStructure<float>((nint)mappedData);
        vk.UnmapMemory(device, outputMemory);

        Console.WriteLine($"Результат: {a} + {b} = {result}");

        // ---------- Очистка ----------
        SilkMarshal.FreeString((nint)entryPointName);
        vk.DestroyPipeline(device, pipeline, null);
        vk.DestroyPipelineLayout(device, pipelineLayout, null);
        vk.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);
        vk.DestroyDescriptorPool(device, descriptorPool, null);
        vk.DestroyCommandPool(device, commandPool, null);
        vk.DestroyBuffer(device, uniformBuffer, null);
        vk.FreeMemory(device, uniformMemory, null);
        vk.DestroyBuffer(device, outputBuffer, null);
        vk.FreeMemory(device, outputMemory, null);
        vk.DestroyShaderModule(device, shaderModule, null);
        vk.DestroyDevice(device, null);
        vk.DestroyInstance(instance, null);
    }
}