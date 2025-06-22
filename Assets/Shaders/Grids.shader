Shader "Custom/Grids"
{
    Properties
    {
        _NoneColor ("None Color", Color) = (0, 0, 0, 1)
        _FloorColor ("Floor Color", Color) = (0.8, 0.8, 0.8, 1)
        _BlockColor ("Block Color", Color) = (0.3, 0.3, 0.3, 1)
        _StartColor ("Start Color", Color) = (0, 1, 0, 1)
        _EndColor ("End Color", Color) = (1, 0, 0, 1)
        _Path1Color ("Path1 Color", Color) = (0, 0, 1, 1)
        _Path2Color ("Path2 Color", Color) = (1, 1, 0, 1)
        _GridColor ("Grid Color", Color) = (0, 0, 0, 0.3)
        _GridThickness ("Grid Thickness", Range(0.001, 0.1)) = 0.01
        _HighlightColor ("Highlight Color", Color) = (1, 1, 0, 1)
        _HighlightThickness ("Highlight Thickness", Range(0.001, 0.1)) = 0.03
        [HideInInspector] _MapSizeSum ("Map Size Sum", Float) = 20
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _NoneColor;
                float4 _FloorColor;
                float4 _BlockColor;
                float4 _StartColor;
                float4 _EndColor;
                float4 _Path1Color;
                float4 _Path2Color;
                float4 _GridColor;
                float _GridThickness;
                float4 _HighlightColor;
                float _HighlightThickness;
                float _MapSizeSum;
            CBUFFER_END

            StructuredBuffer<int> _CellTypes;
            StructuredBuffer<int> _HighlightCells; // 0表示不高亮，1表示高亮
            int _HighlightType;
            int _GridWidth;
            int _GridHeight;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 GetColorByType(int type)
            {
                switch (type)
                {
                    case -1: // None
                        return _NoneColor;
                    case 0: // Floor
                        return _FloorColor;
                    case 1: // Block
                        return _BlockColor;
                    case 2: // Start
                        return _StartColor;
                    case 3: // End
                        return _EndColor;
                    case 4: // Path1
                        return _Path1Color;
                    case 5: // Path2
                        return _Path2Color;
                    default:
                        return _FloorColor;
                }
            }

            float4 frag(Varyings input) : SV_Target
            {
                // 计算当前像素对应的单元格索引
                int cellX = floor(input.uv.x * _GridWidth);
                int cellY = floor(input.uv.y * _GridHeight);
                int cellIndex = cellX * _GridHeight + cellY;

                // 获取单元格类型和高亮状态
                int cellType = _CellTypes[cellIndex];
                int isHighlighted = _HighlightCells[cellIndex];

                // 根据单元格类型和高亮状态选择颜色
                float4 color;
                if (isHighlighted == 1)
                {
                    color = GetColorByType(_HighlightType);
                }
                else
                {
                    color = GetColorByType(cellType);
                }

                // 计算网格线
                float2 gridUV = frac(input.uv * float2(_GridWidth, _GridHeight));
                float adjustedThickness = _GridThickness * min(max(_MapSizeSum, 2.0) / 20.0, 2.5); // 根据地图大小调整线宽
                float2 grid = step(1.0 - adjustedThickness, gridUV) + step(gridUV, float2(adjustedThickness, adjustedThickness));
                float isGrid = saturate(grid.x + grid.y);

                // 计算高亮边框
                float2 cellUV = frac(input.uv * float2(_GridWidth, _GridHeight));

                // 计算相邻格子的索引（直线方向）
                int rightIndex = min(cellX + 1, _GridWidth - 1) * _GridHeight + cellY;
                int leftIndex = max(cellX - 1, 0) * _GridHeight + cellY;
                int topIndex = cellX * _GridHeight + min(cellY + 1, _GridHeight - 1);
                int bottomIndex = cellX * _GridHeight + max(cellY - 1, 0);

                // 计算对角格子的索引
                int topRightIndex = min(cellX + 1, _GridWidth - 1) * _GridHeight + min(cellY + 1, _GridHeight - 1);
                int topLeftIndex = max(cellX - 1, 0) * _GridHeight + min(cellY + 1, _GridHeight - 1);
                int bottomRightIndex = min(cellX + 1, _GridWidth - 1) * _GridHeight + max(cellY - 1, 0);
                int bottomLeftIndex = max(cellX - 1, 0) * _GridHeight + max(cellY - 1, 0);

                // 获取所有相邻格子的高亮状态
                float4 neighborHighlights = float4(
                    _HighlightCells[rightIndex],
                    _HighlightCells[leftIndex],
                    _HighlightCells[topIndex],
                    _HighlightCells[bottomIndex]
                );

                float4 diagonalHighlights = float4(
                    _HighlightCells[topRightIndex],
                    _HighlightCells[topLeftIndex],
                    _HighlightCells[bottomRightIndex],
                    _HighlightCells[bottomLeftIndex]
                );

                // 计算边界高亮
                float2 edgeHighlight = float2(
                    (cellUV.x > (1.0 - adjustedThickness) && neighborHighlights.x) ||
                    (cellUV.x < adjustedThickness && neighborHighlights.y),
                    (cellUV.y > (1.0 - adjustedThickness) && neighborHighlights.z) ||
                    (cellUV.y < adjustedThickness && neighborHighlights.w)
                );

                // 计算顶点高亮（对角相交处）
                float2 cornerDist = float2(
                    min(cellUV.x, 1.0 - cellUV.x),
                    min(cellUV.y, 1.0 - cellUV.y)
                );

                // 检查是否在角落的正方形区域内
                float4 corners = float4(
                    all(float2(cellUV.x >= (1.0 - adjustedThickness), cellUV.y >= (1.0 - adjustedThickness))), // 右上
                    all(float2(cellUV.x <= adjustedThickness, cellUV.y >= (1.0 - adjustedThickness))), // 左上
                    all(float2(cellUV.x >= (1.0 - adjustedThickness), cellUV.y <= adjustedThickness)), // 右下
                    all(float2(cellUV.x <= adjustedThickness, cellUV.y <= adjustedThickness))  // 左下
                );

                float4 cornerHighlights = corners * diagonalHighlights;
                float isCornerHighlight = any(cornerHighlights);

                float isHighlightBorder = saturate(edgeHighlight.x + edgeHighlight.y + isCornerHighlight);

                // 如果当前格子是高亮的，使用完整边框
                if (isHighlighted == 1)
                {
                    float2 highlightBorder = step(1.0 - adjustedThickness, cellUV) +
                                           step(cellUV, float2(adjustedThickness, adjustedThickness));
                    isHighlightBorder = saturate(highlightBorder.x + highlightBorder.y);
                }

                // 应用高亮边框
                float4 finalColor = color;

                // 先检查是否有高亮边框
                if (isHighlightBorder > 0)
                {
                    finalColor = _HighlightColor;
                }
                // 如果没有高亮边框，检查是否有普通网格线
                else if (isGrid > 0)
                {
                    finalColor = _GridColor;
                }

                return finalColor;
            }
            ENDHLSL
        }
    }
}
