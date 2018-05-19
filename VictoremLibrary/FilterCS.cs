using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace VictoremLibrary
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct ComputeConstants
    {
        public float Intensity;
        public Vector3 _padding0;
    }

    public class FilterCS
    {
        Game _game;
        public FilterCS(Game game)
        {
            _game = game;
        }

        public void Blur(ref ShaderResourceView srcTextureSRV, float fIntencity)
        {
            var device = _game.DeviceContext.Device;
            var context = _game.DeviceContext;
            using (var srcTexture = srcTextureSRV.ResourceAs<Texture2D>())
            {
                var desc = srcTexture.Description;
                desc.BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess;
                using (var target = new Texture2D(_game.DeviceContext.Device, desc))
                using (var targetUAV = new UnorderedAccessView(_game.DeviceContext.Device, target))
                using (var targetSRV = new ShaderResourceView(_game.DeviceContext.Device, target))
                using (var target2 = new Texture2D(_game.DeviceContext.Device, desc))
                using (var target2UAV = new UnorderedAccessView(_game.DeviceContext.Device, target2))
                using (var target2SRV = new ShaderResourceView(_game.DeviceContext.Device, target2))

                using (var computeBuffer = new Buffer(_game.DeviceContext.Device,
                             Utilities.SizeOf<ComputeConstants>(),
                             ResourceUsage.Default,
                             BindFlags.ConstantBuffer,
                             CpuAccessFlags.None,
                             ResourceOptionFlags.None, 0))
                using (var horizCS = StaticMetods.GetComputeShader(device, "Shaders\\Filters\\HBlurCS.hlsl", new[] {
                                        new SharpDX.Direct3D.ShaderMacro("THREADSX", 1024),
                                        new SharpDX.Direct3D.ShaderMacro("THREADSY", 1), }))
                using (var vertCS = StaticMetods.GetComputeShader(device, "Shaders\\Filters\\VBlurCS.hlsl", new[] {
                                        new SharpDX.Direct3D.ShaderMacro("THREADSX", 1),
                                        new SharpDX.Direct3D.ShaderMacro("THREADSY", 1024), }))
                {


                    var constants = new ComputeConstants { Intensity = fIntencity };

                    _game.DeviceContext.UpdateSubresource(ref constants, computeBuffer);

                    // The first source resource is the original image 
                    context.ComputeShader.SetShaderResource(0, srcTextureSRV);
                    // The first destination resource is target 
                    context.ComputeShader.SetUnorderedAccessView(0, targetUAV);
                    // Run the horizontal blur first (order doesn't matter) 
                    context.ComputeShader.Set(horizCS);
                    _game.DeviceContext.ComputeShader.SetConstantBuffer(0, computeBuffer);
                    context.Dispatch((int)Math.Ceiling(desc.Width / 1024.0), (int)Math.Ceiling(desc.Height / 1.0), 1);

                    // We must set the compute shader stage SRV and UAV to 
                    // null between calls to the compute shader 
                    context.ComputeShader.SetShaderResource(0, null);
                    context.ComputeShader.SetUnorderedAccessView(0, null);

                    // The second source resource is the first target
                    context.ComputeShader.SetShaderResource(0, targetSRV);
                    // The second destination resource is target2 
                    context.ComputeShader.SetUnorderedAccessView(0, target2UAV);
                    // Run the vertical blur
                    context.ComputeShader.Set(vertCS);
                    _game.DeviceContext.ComputeShader.SetConstantBuffer(0, computeBuffer);
                    context.Dispatch((int)Math.Ceiling(desc.Width / 1.0), (int)Math.Ceiling(desc.Height / 1024.0), 1);

                    // Set the compute shader stage SRV and UAV to null 
                    context.ComputeShader.SetShaderResource(0, null);
                    context.ComputeShader.SetUnorderedAccessView(0, null);
                    _game.DeviceContext.ComputeShader.SetConstantBuffer(0, null);
                    StaticMetods.CopyUAVToSRV(_game.DeviceContext.Device, ref srcTextureSRV, target2UAV);
                }
            }
        }

        public void Desaturate(ref ShaderResourceView srcTextureSRV, float fIntencity)
        {
            srcTextureSRV = ApplyCS(srcTextureSRV, fIntencity, "Shaders\\Filters\\DesaturateCS.hlsl");
        }

        public void Contrast(ref ShaderResourceView srcTextureSRV, float fIntencity)
        {
            srcTextureSRV = ApplyCS(srcTextureSRV, fIntencity, "Shaders\\Filters\\Contrast.hlsl");
        }

        public void Brightness(ref ShaderResourceView srcTextureSRV, float fIntencity)
        {
            srcTextureSRV = ApplyCS(srcTextureSRV, fIntencity, "Shaders\\Filters\\Yarkost.hlsl");
        }

        public void SobelEdgeColor(ref ShaderResourceView srcTextureSRV, float fIntencity)
        {
            srcTextureSRV = ApplyCS(srcTextureSRV, fIntencity, "Shaders\\Filters\\OSobelEdge.hlsl");
        }
        public void SoblerEdge(ref ShaderResourceView srcTextureSRV, float fIntencity)
        {
            srcTextureSRV = ApplyCS(srcTextureSRV, fIntencity, "Shaders\\Filters\\SobelEdge.hlsl");
        }
        public void Sepia(ref ShaderResourceView srcTextureSRV, float fIntencity)
        {
            srcTextureSRV = ApplyCS(srcTextureSRV, fIntencity, "Shaders\\Filters\\Sepia.hlsl");
        }

        public int[] Histogram(ShaderResourceView srcTextureSRV)
        {
            SharpDX.Direct3D.ShaderMacro[] defines = new[] {
                                        new SharpDX.Direct3D.ShaderMacro("THREADSX", 32),
                                        new SharpDX.Direct3D.ShaderMacro("THREADSY", 32), };
            using (var histogramResult = new SharpDX.Direct3D11.Buffer(_game.DeviceContext.Device,
                 new BufferDescription
                 {
                     BindFlags = BindFlags.UnorderedAccess,
                     CpuAccessFlags = CpuAccessFlags.None,
                     OptionFlags = ResourceOptionFlags.BufferAllowRawViews,
                     Usage = ResourceUsage.Default,
                     SizeInBytes = 256 * 4,
                     StructureByteStride = 4
                 }))
            using (var histogramUAV = StaticMetods.CreateBufferUAV(_game.DeviceContext.Device, histogramResult))
            {
                _game.DeviceContext.ClearUnorderedAccessView(histogramUAV, Int4.Zero);

                var cpuReadDesc = histogramResult.Description;
                cpuReadDesc.OptionFlags = ResourceOptionFlags.None;
                cpuReadDesc.BindFlags = BindFlags.None;
                cpuReadDesc.CpuAccessFlags = CpuAccessFlags.Read;
                cpuReadDesc.Usage = ResourceUsage.Staging;

                using (var histogramCPU = new Buffer(_game.DeviceContext.Device, cpuReadDesc))
                using (var srcTexture = srcTextureSRV.ResourceAs<Texture2D>())
                using (var cs = StaticMetods.GetComputeShader(_game.DeviceContext.Device, "Shaders\\Filters\\LumHistorgam.hlsl", defines))
                {
                    var desc = srcTexture.Description;

                    _game.DeviceContext.ComputeShader.SetShaderResource(0, srcTextureSRV);
                    _game.DeviceContext.ComputeShader.SetUnorderedAccessView(0, histogramUAV);
                    _game.DeviceContext.ComputeShader.Set(cs);
                    _game.DeviceContext.Dispatch((int)Math.Ceiling(desc.Width / 1024.0), (int)Math.Ceiling(desc.Height / 1.0), 1);

                    _game.DeviceContext.ComputeShader.SetShaderResource(0, null);
                    _game.DeviceContext.ComputeShader.SetUnorderedAccessView(0, null);

                    // Копировать результат в буффер из которого наш Процессор может читать данные.
                    _game.DeviceContext.CopyResource(histogramResult, histogramCPU);

                    return StaticMetods.GetIntArrayFromByfferData(_game.DeviceContext, histogramCPU);
                }
            }
        }

        private ShaderResourceView ApplyCS(ShaderResourceView srcTextureSRV, float fIntencity, string Shader)
        {
            SharpDX.Direct3D.ShaderMacro[] defines = new[] {
                                        new SharpDX.Direct3D.ShaderMacro("THREADSX", 32),
                                        new SharpDX.Direct3D.ShaderMacro("THREADSY", 32), };
            using (var srcTexture = srcTextureSRV.ResourceAs<Texture2D>())
            {
                var desc = srcTexture.Description;
                desc.BindFlags = BindFlags.UnorderedAccess;
                using (var target = new Texture2D(_game.DeviceContext.Device, desc))
                using (var targetUAV = new UnorderedAccessView(_game.DeviceContext.Device, target))
                using (var computeBuffer = new Buffer(_game.DeviceContext.Device,
                                     Utilities.SizeOf<ComputeConstants>(),
                                     ResourceUsage.Default,
                                     BindFlags.ConstantBuffer,
                                     CpuAccessFlags.None,
                                     ResourceOptionFlags.None,
                                     0))
                using (var cs = StaticMetods.GetComputeShader(_game.DeviceContext.Device, Shader, defines))
                {
                    var constants = new ComputeConstants { Intensity = fIntencity };
                    _game.DeviceContext.UpdateSubresource(ref constants, computeBuffer);

                    _game.DeviceContext.ComputeShader.Set(cs);
                    _game.DeviceContext.ComputeShader.SetShaderResource(0, srcTextureSRV);
                    _game.DeviceContext.ComputeShader.SetUnorderedAccessView(0, targetUAV);
                    _game.DeviceContext.ComputeShader.SetConstantBuffer(0, computeBuffer);

                    _game.DeviceContext.Dispatch((int)Math.Ceiling(desc.Width / 32.0),
                        (int)Math.Ceiling(desc.Height / 32.0), 1);

                    _game.DeviceContext.ComputeShader.SetShaderResource(0, null);
                    _game.DeviceContext.ComputeShader.SetUnorderedAccessView(0, null);
                    _game.DeviceContext.ComputeShader.SetConstantBuffer(0, null);

                    StaticMetods.CopyUAVToSRV(_game.DeviceContext.Device, ref srcTextureSRV, targetUAV);
                }

            }

            return srcTextureSRV;
        }
    }
}
