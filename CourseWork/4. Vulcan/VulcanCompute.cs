using Silk.NET.Vulkan;
using Silk.NET.Core.Native;
using System.Runtime.InteropServices;
using SystemBuffer = System.Buffer;
using Buffer = Silk.NET.Vulkan.Buffer;

public class VulkanCompute
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
        
        float a = 3.5f;
        float b = 2.5f;
        byte[] uniformData = new byte[8];
        SystemBuffer.BlockCopy(BitConverter.GetBytes(a), 0, uniformData, 0, 4);
        SystemBuffer.BlockCopy(BitConverter.GetBytes(b), 0, uniformData, 4, 4);
        // Заполнить uniformBuffer через MapMemory

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

        // ---------- Uniform‑буфер для u_time (одно float) ----------
        const int UniformSize = sizeof(float);
        float uTimeValue = 0.5f; // пример значения

        var bufferCreateInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)UniformSize,
            Usage = BufferUsageFlags.UniformBufferBit | BufferUsageFlags.TransferDstBit,
            SharingMode = SharingMode.Exclusive
        };
        Buffer uniformBuffer;
        vk.CreateBuffer(device, &bufferCreateInfo, null, &uniformBuffer);

        // Память для буфера
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
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = memoryTypeIndex
        };
        DeviceMemory uniformMemory;
        vk.AllocateMemory(device, &allocInfo, null, &uniformMemory);
        vk.BindBufferMemory(device, uniformBuffer, uniformMemory, 0);

        // Заполняем данные
        void* mappedData;
        vk.MapMemory(device, uniformMemory, 0, (ulong)UniformSize, 0, &mappedData);
        Marshal.StructureToPtr(uTimeValue, (nint)mappedData, false);
        vk.UnmapMemory(device, uniformMemory);

        // ---------- Дескрипторы: set=0, binding=0 (Uniform Buffer) ----------
        var descriptorSetLayoutBinding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        var descriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &descriptorSetLayoutBinding
        };
        DescriptorSetLayout descriptorSetLayout;
        vk.CreateDescriptorSetLayout(device, &descriptorSetLayoutCreateInfo, null, &descriptorSetLayout);

        var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &descriptorSetLayout
        };
        PipelineLayout pipelineLayout;
        vk.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, &pipelineLayout);

        // Дескрипторный пул
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.UniformBuffer,
            DescriptorCount = 1
        };
        var descriptorPoolCreateInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = 1,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize
        };
        DescriptorPool descriptorPool;
        vk.CreateDescriptorPool(device, &descriptorPoolCreateInfo, null, &descriptorPool);

        var setAllocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &descriptorSetLayout
        };
        DescriptorSet descriptorSet;
        vk.AllocateDescriptorSets(device, &setAllocInfo, &descriptorSet);

        // Связываем буфер
        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = uniformBuffer,
            Offset = 0,
            Range = (ulong)UniformSize
        };
        var writeDescriptor = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = descriptorSet,
            DstBinding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            PBufferInfo = &bufferInfo
        };
        vk.UpdateDescriptorSets(device, 1, &writeDescriptor, 0, null);

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

        // Запускаем одну рабочую группу (64 потока, local_size_x=64)
        vk.CmdDispatch(commandBuffer, 1, 1, 1);

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

        Console.WriteLine("Shader executed successfully.");

        // ---------- Очистка ----------
        SilkMarshal.FreeString((nint)entryPointName);
        vk.DestroyPipeline(device, pipeline, null);
        vk.DestroyPipelineLayout(device, pipelineLayout, null);
        vk.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);
        vk.DestroyDescriptorPool(device, descriptorPool, null);
        vk.DestroyCommandPool(device, commandPool, null);
        vk.DestroyBuffer(device, uniformBuffer, null);
        vk.FreeMemory(device, uniformMemory, null);
        vk.DestroyShaderModule(device, shaderModule, null);
        vk.DestroyDevice(device, null);
        vk.DestroyInstance(instance, null);
    }
}