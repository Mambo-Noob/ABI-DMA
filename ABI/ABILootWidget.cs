using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace ImGuiOverlay.ABI
{
    internal static class ABILootWidget
    {
        private static bool _open = true;

        public static void Render(ABIGameConfig cfg, Vector3 localPos)
        {
            if (!cfg.DrawLootWidget) return;

            ImGui.SetNextWindowPos(cfg.LootWidgetPosition, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(480, 420), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(cfg.LootWidgetBackground.W);

            if (!ImGui.Begin("  Loot", ref _open, ImGuiWindowFlags.NoCollapse)) { ImGui.End(); return; }
            cfg.LootWidgetPosition = ImGui.GetWindowPos();

            if (!ABILoot.TryGetLoot(out var frame) || frame.Items == null)
            {
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "[!] No loot frame yet");
                ImGui.End();
                if (!_open) { cfg.DrawLootWidget = false; _open = true; }
                return;
            }

            // Bias applied at render time (positions stored raw in ABILoot)
            Vector3 bias = ABIPlayers.GetOriginBias();

            double ageMs = (System.Diagnostics.Stopwatch.GetTimestamp() - frame.StampTicks)
                           * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            ImGui.TextColored(new Vector4(0.4f, 1f, 0.4f, 1f),
                $"Age: {ageMs:F0}ms  Actors: {frame.TotalActorsSeen}  Items: {frame.Items.Count}  Containers: {frame.Containers?.Count ?? 0}");
            ImGui.Separator();

            if (ImGui.BeginTabBar("loot_tabs"))
            {
                if (ImGui.BeginTabItem("Ground Items"))  { DrawItemsTab(frame, cfg, localPos, bias);      ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem("Containers"))    { DrawContainersTab(frame, cfg, localPos, bias); ImGui.EndTabItem(); }
                ImGui.EndTabBar();
            }

            ImGui.Separator();
            ImGui.TextColored(Dim, "Drag to move  Resize from corners  Close to disable");
            ImGui.End();
            if (!_open) { cfg.DrawLootWidget = false; _open = true; }
        }

        private static void DrawItemsTab(ABILoot.Frame frame, ABIGameConfig cfg, Vector3 localPos, Vector3 bias)
        {
            var sane = frame.Items
                .Where(x => x.StandardPrice >= 0 && x.StandardPrice < 100_000_000)
                .ToList();

            int zeroCnt  = sane.Count(x => x.StandardPrice == 0);
            int belowCnt = sane.Count(x => x.StandardPrice > 0 && x.StandardPrice < cfg.LootWidgetMinPrice);

            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.4f, 1f),
                $"Min price: ?{cfg.LootWidgetMinPrice:N0}   Below: {belowCnt}   No price: {zeroCnt}");

            var valuable = sane
                .Where(x => x.StandardPrice >= cfg.LootWidgetMinPrice)
                .OrderByDescending(x => x.StandardPrice)
                .Take(cfg.LootWidgetMaxItems)
                .ToList();

            if (valuable.Count == 0)
            {
                var any = sane.OrderByDescending(x => x.StandardPrice).Take(10).ToList();
                if (any.Count == 0) { ImGui.TextColored(Warn, "[!] No items above filter."); return; }
                ImGui.TextColored(Warn, $"None above filter. Showing top {any.Count}:");
                ImGui.Separator();
                RenderItemCols("lwraw", any, cfg, localPos, bias);
                return;
            }

            long total = valuable.Sum(x => (long)Math.Clamp(x.StandardPrice, 0, 100_000_000));
            ImGui.TextColored(Gold, $"Top {valuable.Count}  Total: ?{total:N0}");
            ImGui.Separator();
            RenderItemCols("lwcols", valuable, cfg, localPos, bias);
        }

        private static void RenderItemCols(string id, List<ABILoot.Item> items,
            ABIGameConfig cfg, Vector3 localPos, Vector3 bias)
        {
            ImGui.Columns(4, id, true);
            ImGui.SetColumnWidth(0, 200); ImGui.SetColumnWidth(1, 80);
            ImGui.SetColumnWidth(2, 80);  ImGui.SetColumnWidth(3, 70);
            ImGui.TextColored(Dim, "Item");  ImGui.NextColumn();
            ImGui.TextColored(Dim, "Price"); ImGui.NextColumn();
            ImGui.TextColored(Dim, "Sell");  ImGui.NextColumn();
            ImGui.TextColored(Dim, "Dist");  ImGui.NextColumn();
            ImGui.Separator();

            foreach (var item in items)
            {
                Vector3 wpos = item.Position + bias;
                float   dist = localPos != default ? Vector3.Distance(localPos, wpos) / 100f : -1f;
                string  name = Trunc(BestItemLabel(item.Label, item.ClassName), 28);

                Vector4 col = item.StandardPrice >= cfg.LootMinPriceImportant ? cfg.LootWidgetImportantColor
                    : dist >= 0 && dist < 50f ? new Vector4(0.4f, 1f, 0.4f, 1f)
                    : new Vector4(0.85f, 0.85f, 0.85f, 1f);

                ImGui.TextColored(col, name);                                                                       ImGui.NextColumn();
                ImGui.TextColored(col, item.StandardPrice == 0 ? "-" : $"?{item.StandardPrice:N0}");               ImGui.NextColumn();
                ImGui.TextColored(col, item.SellPrice == 0 ? "-" : $"?{item.SellPrice:N0}");                      ImGui.NextColumn();
                ImGui.TextColored(col, !cfg.LootWidgetShowDistance ? "" : dist < 0 ? "?" : $"{dist:F0}m");         ImGui.NextColumn();
            }
            ImGui.Columns(1);
        }

        private static void DrawContainersTab(ABILoot.Frame frame, ABIGameConfig cfg, Vector3 localPos, Vector3 bias)
        {
            if (frame.Containers == null || frame.Containers.Count == 0)
            { ImGui.TextColored(Warn, "No containers found."); return; }

            var sorted = frame.Containers
                .OrderBy(c => Vector3.Distance(localPos, c.Position + bias))
                .Take(cfg.LootWidgetMaxItems)
                .ToList();

            ImGui.TextColored(Dim, $"{frame.Containers.Count} containers  Showing {sorted.Count} nearest");
            ImGui.Separator();

            ImGui.Columns(4, "ctrcols", true);
            ImGui.SetColumnWidth(0, 180); ImGui.SetColumnWidth(1, 60);
            ImGui.SetColumnWidth(2, 100); ImGui.SetColumnWidth(3, 80);
            ImGui.TextColored(Dim, "Container"); ImGui.NextColumn();
            ImGui.TextColored(Dim, "Items");     ImGui.NextColumn();
            ImGui.TextColored(Dim, "Value");     ImGui.NextColumn();
            ImGui.TextColored(Dim, "Dist");      ImGui.NextColumn();
            ImGui.Separator();

            foreach (var c in sorted)
            {
                Vector3 wpos   = c.Position + bias;
                float   dist   = localPos != default ? Vector3.Distance(localPos, wpos) / 100f : -1f;
                int     cnt    = c.Contents?.Count ?? 0;
                int     val    = c.Contents?.Sum(x => x.StandardPrice) ?? 0;
                bool    empty  = cnt == 0;
                bool    hasHi  = !empty && c.Contents!.Any(x => x.StandardPrice >= cfg.LootMinPriceImportant);
                string  name   = Trunc(BestItemLabel(c.Label, c.ClassName), 24);

                Vector4 col = hasHi ? cfg.LootWidgetImportantColor : !empty ? Gold : Dim;

                ImGui.TextColored(col, c.IsRolledUp ? $"[R] {name}" : name);   ImGui.NextColumn();
                ImGui.TextColored(col, empty ? "-" : cnt.ToString());           ImGui.NextColumn();
                ImGui.TextColored(col, val == 0 ? "-" : $"?{val:N0}");        ImGui.NextColumn();
                ImGui.TextColored(col, !cfg.LootWidgetShowDistance ? "" : dist < 0 ? "?" : $"{dist:F0}m"); ImGui.NextColumn();

                if (!empty && dist >= 0 && dist < 50f && hasHi)
                {
                    foreach (var child in c.Contents!.Where(x => x.StandardPrice >= cfg.LootMinPriceImportant).Take(3))
                    {
                        string cn = Trunc("  > " + (BestItemLabel(child.Label, child.ClassName)), 24);
                        ImGui.TextColored(cfg.LootWidgetImportantColor, cn);                                        ImGui.NextColumn();
                        ImGui.TextColored(cfg.LootWidgetImportantColor, child.Stack > 1 ? $"x{child.Stack}" : ""); ImGui.NextColumn();
                        ImGui.TextColored(cfg.LootWidgetImportantColor, $"?{child.StandardPrice:N0}");             ImGui.NextColumn();
                        ImGui.NextColumn();
                    }
                }
            }
            ImGui.Columns(1);
        }

        private static readonly Vector4 Dim  = new(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Vector4 Warn = new(1f,    0.65f, 0.15f, 1f);
        private static readonly Vector4 Gold = new(1f,    0.84f, 0f,    1f);
        private static string Trunc(string s, int max) => s.Length > max ? s[..(max - 3)] + "..." : s;
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