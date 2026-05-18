using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace ImGuiOverlay.ABI
{
    internal static class ABILootESP
    {
        private const int MAX_ITEMS      = 300;
        private const int MAX_CONTAINERS = 100;
        private const int VERTEX_BUDGET  = 60_000;

        private static int _vtx;

        public static void Render(ABIGameConfig cfg, Vector3 localPos,
                                   FMinimalViewInfo cam, float zoom)
        {
            if (!ABILoot.TryGetLoot(out var frame) || frame.Items == null) return;
            if (cam.Fov <= 0f || !float.IsFinite(cam.Location.X)) return;

            var io = ImGui.GetIO();
            float W = io.DisplaySize.X, H = io.DisplaySize.Y;
            var dl  = ImGui.GetBackgroundDrawList();
            _vtx = 0;

            // Bias is added at render time (positions stored raw in ABILoot)
            Vector3 bias = ABIPlayers.GetOriginBias();

            if (cfg.DrawGroundLoot)
            {
                var filtered = Filter(frame.Items, cfg, localPos, bias);
                int drawn = 0;
                foreach (var item in filtered)
                {
                    if (drawn++ >= MAX_ITEMS || _vtx >= VERTEX_BUDGET) break;
                    DrawItem(item, cfg, localPos, cam, zoom, dl, W, H, bias);
                }
            }

            if (cfg.DrawContainers && frame.Containers != null && _vtx < VERTEX_BUDGET)
            {
                var sorted = frame.Containers
                    .Where(c => cfg.DrawEmptyContainers || (c.Contents != null && c.Contents.Count > 0))
                    .OrderBy(c => Vector3.DistanceSquared(localPos, c.Position + bias))
                    .Take(MAX_CONTAINERS);

                int drawn = 0;
                foreach (var c in sorted)
                {
                    if (drawn++ >= MAX_CONTAINERS || _vtx >= VERTEX_BUDGET) break;
                    float d = Vector3.Distance(localPos, c.Position + bias) / 100f;
                    if (d > cfg.ContainerMaxDistance) continue;
                    DrawContainer(c, cfg, localPos, cam, zoom, dl, W, H, bias);
                }
            }
        }

        // ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        //  Filtering
        // ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        private static List<ABILoot.Item> Filter(List<ABILoot.Item> items,
            ABIGameConfig cfg, Vector3 localPos, Vector3 bias)
        {
            var includes = ParseTerms(cfg.LootFilterInclude);
            var excludes = ParseTerms(cfg.LootFilterExclude);

            var out_ = new List<(ABILoot.Item item, float distSq, int priority)>(items.Count);
            foreach (var item in items)
            {
                float distM = Vector3.Distance(localPos, item.Position + bias) / 100f;
                if (distM > cfg.GroundLootMaxDistance) continue;
                if (item.StandardPrice < cfg.LootMinPriceRegular) continue;
                if (includes.Count > 0 && !MatchesAny(item, includes)) continue;
                if (excludes.Count > 0 &&  MatchesAny(item, excludes)) continue;

                int pri = item.StandardPrice >= cfg.LootMinPriceImportant ? 2 : (distM < 50f ? 1 : 0);
                out_.Add((item, distM * distM, pri));
            }

            return out_
                .OrderByDescending(x => x.priority)
                .ThenBy(x => x.distSq)
                .Take(MAX_ITEMS)
                .Select(x => x.item)
                .ToList();
        }

        private static bool MatchesAny(ABILoot.Item item, List<string> terms)
        {
            foreach (var t in terms)
                if ((item.ClassName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (item.Label    ?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false))
                    return true;
            return false;
        }

        private static List<string> ParseTerms(string filter)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(filter)) return result;
            foreach (var t in filter.Split(',', StringSplitOptions.RemoveEmptyEntries))
            { var s = t.Trim(); if (s.Length > 0) result.Add(s); }
            return result;
        }

        // ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        //  Draw: ground item
        // ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        private static void DrawItem(ABILoot.Item item, ABIGameConfig cfg, Vector3 localPos,
            FMinimalViewInfo cam, float zoom, ImDrawListPtr dl, float W, float H, Vector3 bias)
        {
            Vector3 wpos = item.Position + bias;
            if (!ABIMath.WorldToScreen(wpos, cam, W, H, zoom, out var screen)) return;

            float distM     = Vector3.Distance(localPos, wpos) / 100f;
            bool  important = item.StandardPrice >= cfg.LootMinPriceImportant;
            uint  col       = ImGui.ColorConvertFloat4ToU32(important ? cfg.LootImportantColor : cfg.LootRegularColor);
            float sz        = cfg.GroundLootMarkerSize;

            if (distM < 100f)
            {
                if (important) { DrawStar(dl, screen, sz, col);    _vtx += 24; }
                else           { DrawDiamond(dl, screen, sz, col); _vtx += 20; }
            }
            else
            {
                dl.AddCircleFilled(screen, sz * 0.6f, col);
                _vtx += 24;
            }

            if (distM < 100f && cfg.DrawGroundLootNames)
            {
                string lbl = BestItemLabel(item.Label, item.ClassName);
                if (lbl.Length > 30) lbl = lbl[..27] + "...";
                if (distM < 50f && cfg.LootShowPrice && item.StandardPrice > 0)
                    lbl = $"{lbl} (?{item.StandardPrice:N0})";
                if (cfg.DrawGroundLootDistance) lbl = $"{lbl} [{distM:F0}m]";
                DrawTextBg(dl, screen, sz, lbl, col);
                _vtx += 6 + lbl.Length * 6;
            }
        }

        // ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        //  Draw: container
        // ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        private static void DrawContainer(ABILoot.Container c, ABIGameConfig cfg,
            Vector3 localPos, FMinimalViewInfo cam, float zoom,
            ImDrawListPtr dl, float W, float H, Vector3 bias)
        {
            Vector3 wpos = c.Position + bias;
            if (!ABIMath.WorldToScreen(wpos, cam, W, H, zoom, out var screen)) return;

            float distM   = Vector3.Distance(localPos, wpos) / 100f;
            bool  empty   = c.Contents == null || c.Contents.Count == 0;
            bool  hasHigh = !empty && c.Contents!.Any(x => x.StandardPrice >= cfg.LootMinPriceImportant);

            var  col4 = hasHigh ? cfg.LootImportantColor
                      : !empty  ? cfg.ContainerFilledColor
                      :           cfg.ContainerEmptyColor;
            uint col  = ImGui.ColorConvertFloat4ToU32(col4);
            float half = cfg.ContainerMarkerSize / 2f;

            dl.AddRectFilled(new Vector2(screen.X - half, screen.Y - half),
                             new Vector2(screen.X + half, screen.Y + half), col);
            dl.AddRect      (new Vector2(screen.X - half, screen.Y - half),
                             new Vector2(screen.X + half, screen.Y + half),
                             0xE6000000, 0, ImDrawFlags.None, 2f);
            _vtx += 12;

            if (cfg.DrawContainerNames && distM < 150f)
            {
                string name = BestItemLabel(c.Label, c.ClassName);
                if (name.Length > 20) name = name[..17] + "...";
                int cnt = c.Contents?.Count ?? 0;
                string lbl = empty ? $"{name} (Empty)" : $"{name} ({cnt})";
                if (distM < 80f && cfg.LootShowPrice)
                {
                    int val = c.Contents?.Sum(x => x.StandardPrice) ?? 0;
                    if (val > 0) lbl = $"{lbl} ?{val:N0}";
                }
                if (cfg.DrawContainerDistance) lbl = $"{lbl} [{distM:F0}m]";
                DrawTextBg(dl, screen, cfg.ContainerMarkerSize, lbl, col);
                _vtx += 6 + lbl.Length * 6;
            }

            if (!empty && distM < 40f && cfg.DrawGroundLootNames && _vtx < VERTEX_BUDGET)
            {
                var notable = c.Contents!.Where(x => x.StandardPrice >= cfg.LootMinPriceImportant).Take(3);
                float offsetY = cfg.ContainerMarkerSize + 14f;
                foreach (var child in notable)
                {
                    string childLbl = BestItemLabel(child.Label, child.ClassName);
                    if (childLbl.Length > 24) childLbl = childLbl[..21] + "...";
                    if (cfg.LootShowPrice && child.StandardPrice > 0)
                        childLbl = $"  {childLbl} ?{child.StandardPrice:N0}";
                    var ts  = ImGui.CalcTextSize(childLbl);
                    var pos = new Vector2(screen.X - ts.X / 2, screen.Y + offsetY);
                    dl.AddRectFilled(new Vector2(pos.X - 2, pos.Y - 1),
                                     new Vector2(pos.X + ts.X + 2, pos.Y + ts.Y + 1), 0xB2000000);
                    dl.AddText(pos, ImGui.ColorConvertFloat4ToU32(cfg.LootImportantColor), childLbl);
                    offsetY += ts.Y + 2f;
                    _vtx += 6 + childLbl.Length * 6;
                }
            }
        }

        // ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        //  Helpers
        // ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        private static void DrawTextBg(ImDrawListPtr dl, Vector2 screen, float sz, string text, uint col)
        {
            var ts  = ImGui.CalcTextSize(text);
            var pos = new Vector2(screen.X - ts.X / 2, screen.Y + sz + 4);
            dl.AddRectFilled(new Vector2(pos.X - 2, pos.Y - 1),
                             new Vector2(pos.X + ts.X + 2, pos.Y + ts.Y + 1), 0xB2000000);
            dl.AddText(pos, col, text);
        }

        private static void DrawDiamond(ImDrawListPtr dl, Vector2 c, float size, uint color)
        {
            Vector2[] pts = { new(c.X, c.Y - size), new(c.X + size, c.Y),
                              new(c.X, c.Y + size),  new(c.X - size, c.Y) };
            dl.AddConvexPolyFilled(ref pts[0], pts.Length, color);
            dl.AddPolyline(ref pts[0], pts.Length, 0xE6000000, ImDrawFlags.Closed, 1.5f);
        }

        private static void DrawStar(ImDrawListPtr dl, Vector2 c, float size, uint color)
        {
            var pts = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float a = (i * MathF.PI / 4f) - MathF.PI / 2f;
                float r = (i % 2 == 0) ? size : size * 0.4f;
                pts[i] = new Vector2(c.X + MathF.Cos(a) * r, c.Y + MathF.Sin(a) * r);
            }
            dl.AddConvexPolyFilled(ref pts[0], pts.Length, color);
            dl.AddPolyline(ref pts[0], pts.Length, 0xE6000000, ImDrawFlags.Closed, 1.5f);
        }
        // Best display label: filters UE4 ??? placeholders, falls back to prettified class name
        private static string BestItemLabel(string label, string cls)
        {
            if (!string.IsNullOrWhiteSpace(label) && !label.All(c => c == '?'))
                return label;
            if (!string.IsNullOrWhiteSpace(cls) && !cls.All(c => c == '?'))
            {
                string s = cls;
                int last = s.LastIndexOf('_');
                if (last > 0) { string suf = s[(last+1)..]; bool d = suf.Length > 0; foreach (char c in suf) if (!char.IsDigit(c)) { d = false; break; } if (d) s = s[..last]; }
                if (s.EndsWith("_C", StringComparison.Ordinal)) s = s[..^2];
                if (s.StartsWith("BP_", StringComparison.Ordinal)) s = s[3..];
                s = s.Replace('_', ' ').Trim();
                if (!string.IsNullOrEmpty(s) && !s.All(c => c == '?')) return s;
            }
            return "Item";
        }
    }
}