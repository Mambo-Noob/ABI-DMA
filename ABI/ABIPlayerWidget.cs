using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace ImGuiOverlay.ABI
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI PLAYER WIDGET  ¡¤  Debug overlay listing every tracked actor with all
    //  per-player fields so you can verify reads are correct in-game.
    //
    //  Columns:
    //    # | Type | Dist | Pos (X,Y,Z) | HP | HP Max | Dead | Vis | HasSkel |
    //      Pawn ptr | Root ptr | Mesh ptr | ASC ptr | DeathComp ptr
    // /////////////////////////////////////////////////////////////////////////////////////

    internal static class ABIPlayerWidget
    {
        private static bool   _open       = true;
        private static bool   _showPtrs   = false;
        private static bool   _showRawPos = false;
        private static string _filterText = "";

        public static bool Enabled = true;

        public static void Render(ABIGameConfig cfg)
        {
            if (!Enabled) return;

            ImGui.SetNextWindowPos(new Vector2(20, 20), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(900, 500), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.88f);

            if (!ImGui.Begin("Player Debug", ref _open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.HorizontalScrollbar))
            { ImGui.End(); return; }

            // ©¤©¤ Header / world state ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
            bool worldOk  = ABIPlayers.UWorld != 0;
            bool camOk    = ABIPlayers.Camera.Fov > 0f && float.IsFinite(ABIPlayers.Camera.Location.X);
            bool actorsOk = ABIPlayers.ActorArray != 0 && ABIPlayers.ActorCount > 0;

            ImGui.TextColored(worldOk  ? Green : Red,  $"UWorld: 0x{ABIPlayers.UWorld:X}");
            ImGui.SameLine(220);
            ImGui.TextColored(actorsOk ? Green : Red,
                $"ActorArray: 0x{ABIPlayers.ActorArray:X}  Count: {ABIPlayers.ActorCount}");
            ImGui.SameLine(620);
            ImGui.TextColored(camOk ? Green : Red,
                $"Cam FOV: {ABIPlayers.Camera.Fov:F1}  Loc: {Vec3Str(ABIPlayers.Camera.Location)}");

            ImGui.TextColored(Dim,
                $"LocalPos: {Vec3Str(ABIPlayers.LocalPosition)}   " +
                $"PlayerController: 0x{ABIPlayers.PlayerController:X}   " +
                $"LocalPawn: 0x{ABIPlayers.LocalPawn:X}");

            ImGui.Separator();

            // ©¤©¤ Options row ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
            ImGui.Checkbox("Show Ptrs##pw",    ref _showPtrs);
            ImGui.SameLine();
            ImGui.Checkbox("Show Raw Pos##pw", ref _showRawPos);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(160);
            ImGui.InputText("Filter##pwf", ref _filterText, 64);

            ImGui.Separator();

            // ©¤©¤ Snapshot ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
            bool hasFrame = ABIPlayers.TryGetFrame(out var fr);

            List<ABIPlayers.ABIPlayer> actors;
            lock (ABIPlayers.Sync)
                actors = new List<ABIPlayers.ABIPlayer>(ABIPlayers.ActorList);

            if (actors.Count == 0)
            {
                ImGui.TextColored(Red, "[!] ActorList is empty ¡ª world/players not enumerated yet");
                ImGui.End();
                if (!_open) { Enabled = false; _open = true; }
                return;
            }

            // Build pos lookup
            var posMap = new Dictionary<ulong, ABIPlayers.ActorPos>(fr.Positions?.Count ?? 0);
            if (hasFrame && fr.Positions != null)
                foreach (var ap in fr.Positions) posMap[ap.Pawn] = ap;

            // Column widths depend on options
            int colCount = _showPtrs ? 15 : (_showRawPos ? 13 : 10);

            // ©¤©¤ Table ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
            var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
                             ImGuiTableFlags.ScrollY  | ImGuiTableFlags.SizingFixedFit |
                             ImGuiTableFlags.Resizable;

            float tableH = ImGui.GetContentRegionAvail().Y - 24f;
            if (!ImGui.BeginTable("pwcols", colCount, tableFlags, new Vector2(0, tableH)))
            { ImGui.End(); return; }

            // Column setup
            ImGui.TableSetupScrollFreeze(0, 1); // freeze header row
            ImGui.TableSetupColumn("#",       ImGuiTableColumnFlags.WidthFixed,  28f);
            ImGui.TableSetupColumn("Type",    ImGuiTableColumnFlags.WidthFixed,  48f);
            ImGui.TableSetupColumn("Dist(m)", ImGuiTableColumnFlags.WidthFixed,  64f);
            ImGui.TableSetupColumn("HP",      ImGuiTableColumnFlags.WidthFixed,  52f);
            ImGui.TableSetupColumn("HPMax",   ImGuiTableColumnFlags.WidthFixed,  56f);
            ImGui.TableSetupColumn("Dead",    ImGuiTableColumnFlags.WidthFixed,  40f);
            ImGui.TableSetupColumn("Vis",     ImGuiTableColumnFlags.WidthFixed,  40f);
            ImGui.TableSetupColumn("Skel",    ImGuiTableColumnFlags.WidthFixed,  40f);
            ImGui.TableSetupColumn("HasSet",  ImGuiTableColumnFlags.WidthFixed,  48f);
            if (_showRawPos || _showPtrs)
            {
                ImGui.TableSetupColumn("Pos X", ImGuiTableColumnFlags.WidthFixed, 78f);
                ImGui.TableSetupColumn("Pos Y", ImGuiTableColumnFlags.WidthFixed, 78f);
                ImGui.TableSetupColumn("Pos Z", ImGuiTableColumnFlags.WidthFixed, 78f);
            }
            if (_showPtrs)
            {
                ImGui.TableSetupColumn("Pawn", ImGuiTableColumnFlags.WidthFixed, 128f);
                ImGui.TableSetupColumn("Mesh", ImGuiTableColumnFlags.WidthFixed, 128f);
                ImGui.TableSetupColumn("Root", ImGuiTableColumnFlags.WidthFixed, 128f);
            }
            ImGui.TableHeadersRow();

            int row = 0;
            foreach (var actor in actors)
            {
                string typeLabel = actor.IsBot ? "BOT" : "PMC";

                // Filter
                if (_filterText.Length > 0 &&
                    !typeLabel.Contains(_filterText, StringComparison.OrdinalIgnoreCase) &&
                    !actor.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                    continue;

                posMap.TryGetValue(actor.Pawn, out var ap);
                bool hasPos  = ap.Pawn != 0;
                bool hasSkel = ABIPlayers.TryGetSkeleton(actor.Pawn, out _);
                bool hasSet  = hasPos && ap.HealthMax > 0f;

                float distM = hasPos
                    ? Vector3.Distance(ABIPlayers.LocalPosition, ap.Position) / 100f
                    : -1f;

                Vector4 rowColor = ap.IsDead ? Dead
                    : actor.IsBot            ? Blue
                                             : Orange;

                row++;
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextColored(Dim, $"{row}");
                ImGui.TableNextColumn(); ImGui.TextColored(rowColor, typeLabel);
                ImGui.TableNextColumn(); ImGui.TextColored(rowColor, distM < 0 ? "?" : $"{distM:F1}");

                // HP
                ImGui.TableNextColumn();
                if (hasSet) ImGui.TextColored(HpColor(ap.Health, ap.HealthMax), $"{ap.Health:F0}");
                else        ImGui.TextColored(Dim, "¡ª");

                ImGui.TableNextColumn();
                if (hasSet) ImGui.TextColored(Dim, $"{ap.HealthMax:F0}");
                else        ImGui.TextColored(Dim, "¡ª");

                // Dead
                ImGui.TableNextColumn();
                ImGui.TextColored(ap.IsDead ? Red : Green, ap.IsDead ? "YES" : "no");

                // Vis
                ImGui.TableNextColumn();
                ImGui.TextColored(ap.IsVisible ? Green : Dim, ap.IsVisible ? "YES" : "no");

                // Skel
                ImGui.TableNextColumn();
                ImGui.TextColored(hasSkel ? Green : Dim, hasSkel ? "YES" : "no");

                // HealthSet
                ImGui.TableNextColumn();
                ImGui.TextColored(hasSet ? Green : Red, hasSet ? "YES" : "NO");

                if (_showRawPos || _showPtrs)
                {
                    if (hasPos)
                    {
                        ImGui.TableNextColumn(); ImGui.TextColored(Dim, $"{ap.Position.X:F0}");
                        ImGui.TableNextColumn(); ImGui.TextColored(Dim, $"{ap.Position.Y:F0}");
                        ImGui.TableNextColumn(); ImGui.TextColored(Dim, $"{ap.Position.Z:F0}");
                    }
                    else
                    {
                        ImGui.TableNextColumn(); ImGui.TextColored(Dim, "¡ª");
                        ImGui.TableNextColumn(); ImGui.TextColored(Dim, "¡ª");
                        ImGui.TableNextColumn(); ImGui.TextColored(Dim, "¡ª");
                    }
                }

                if (_showPtrs)
                {
                    ImGui.TableNextColumn(); ImGui.TextColored(actor.Pawn != 0 ? Dim : Red, $"{actor.Pawn:X}");
                    ImGui.TableNextColumn(); ImGui.TextColored(actor.Mesh != 0 ? Dim : Red, $"{actor.Mesh:X}");
                    ImGui.TableNextColumn(); ImGui.TextColored(actor.Root != 0 ? Dim : Red, $"{actor.Root:X}");
                }
            }

            ImGui.EndTable();
            ImGui.Separator();
            ImGui.TextColored(Dim, $"Total actors: {actors.Count}  Frame: {(hasFrame ? "OK" : "MISS")}  " +
                $"Frame stamp age: {(hasFrame ? $"{(System.Diagnostics.Stopwatch.GetTimestamp() - fr.Stamp) * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F0}ms" : "¡ª")}");

            ImGui.End();
            if (!_open) { Enabled = false; _open = true; }
        }

        // ©¤©¤ Helpers ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        private static Vector4 HpColor(float hp, float max)
        {
            float pct = max > 0 ? hp / max : 0f;
            return pct > 0.6f ? Green : pct > 0.3f ? new Vector4(1f, 0.8f, 0.1f, 1f) : Red;
        }

        private static string Vec3Str(Vector3 v)
            => float.IsFinite(v.X) ? $"({v.X:F0},{v.Y:F0},{v.Z:F0})" : "(invalid)";

        private static readonly Vector4 Green  = new(0.2f, 1f,   0.2f, 1f);
        private static readonly Vector4 Red    = new(1f,   0.25f,0.25f,1f);
        private static readonly Vector4 Orange = new(1f,   0.5f, 0.1f, 1f);
        private static readonly Vector4 Blue   = new(0.2f, 0.6f, 1f,   1f);
        private static readonly Vector4 Dead   = new(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Vector4 Dim    = new(0.55f,0.55f,0.55f,1f);
    }
}