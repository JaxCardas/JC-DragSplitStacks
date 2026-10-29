using System.Collections.Generic;
using HarmonyLib;
using InControl;
using UnityEngine;

namespace JC.DragSplitStacks
{
    // Dzielenie trzymanego stosu przeciaganiem po slotach (jak w Minecraft):
    //   LMB przytrzymany i przeciagniety po kilku slotach -> stos dzielony po rowno, reszta zostaje w rece,
    //   RMB przytrzymany i przeciagniety -> po 1 sztuce na kazdy slot.
    // Dziala w kazdej siatce XUiC_ItemStackGrid (plecak, pasek, skrzynie, stacje robocze).
    //
    // Vanilla odklada stos tylko, gdy wcisniecie i puszczenie sa na tym samym slocie (XUiC_ItemStack.Update,
    // pole lastClicked). NGUI zamraza "hover" przy wcisnietym przycisku, wiec slot pod kursorem wyznaczamy
    // sami: promien z kamery UI przez kursor i BoxCollider widoku slotu.
    public static class DragSplit
    {
        private static readonly List<XUiC_ItemStack> visited = new List<XUiC_ItemStack>();
        private static readonly HashSet<XUiC_ItemStack> rejected = new HashSet<XUiC_ItemStack>();
        private static readonly Dictionary<XUiC_ItemStack, bool> holdingBefore = new Dictionary<XUiC_ItemStack, bool>();
        private static bool active;
        private static UICamera.MouseButton button;
        private static int suppressUntilFrame = -1;

        // Gdy true, vanilla nie moze wykonac odlozenia/upuszczenia na puszczeniu przycisku.
        public static bool SuppressVanilla => (active && visited.Count >= 2) || Time.frameCount <= suppressUntilFrame;

        public static void Tick(XUiC_DragAndDropWindow dnd)
        {
            LocalPlayerUI ui = dnd.xui?.playerUI;
            if (ui == null)
                return;
            CursorControllerAbs cursor = ui.CursorController;
            if (cursor == null)
                return;

            if (!active)
            {
                TryStart(dnd, ui, cursor);
                return;
            }

            if (dnd.InMenu && !dnd.IsEmpty() && cursor.GetMouseButton(button))
            {
                XUiC_ItemStack slot = SlotUnderCursor(ui, cursor);
                if (slot != null && !visited.Contains(slot) && !rejected.Contains(slot))
                {
                    if (Accepts(slot, dnd.CurrentStack, dnd))
                        Add(slot);
                    else
                        rejected.Add(slot);
                }
                return;
            }

            // Przycisk puszczony (albo okno zamkniete / reka pusta). Jeden slot = zwykle klikniecie vanilla.
            if (visited.Count >= 2 && dnd.InMenu && !dnd.IsEmpty() && cursor.GetMouseButtonUp(button) && !InputUtils.ShiftKeyPressed)
            {
                Distribute(dnd);
                // Kolejnosc Update okna i slotow w tej klatce nie jest znana - blokujemy vanilla tez w nastepnej.
                suppressUntilFrame = Time.frameCount + 1;
            }
            End();
        }

        public static void Reset() => End();

        private static void TryStart(XUiC_DragAndDropWindow dnd, LocalPlayerUI ui, CursorControllerAbs cursor)
        {
            if (!dnd.InMenu || dnd.IsEmpty())
                return;
            bool left = cursor.GetMouseButtonDown(UICamera.MouseButton.LeftButton);
            bool right = cursor.GetMouseButtonDown(UICamera.MouseButton.RightButton);
            if (left == right)
                return;
            if (ui.playerInput?.GUIActions == null || ui.playerInput.GUIActions.LastInputType == BindingSourceType.DeviceBindingSource)
                return;
            if (InputUtils.ShiftKeyPressed)
                return;
            ItemStack held = dnd.CurrentStack;
            if (held.IsEmpty() || held.count < 2 || held.itemValue.HasQuality || !(held.itemValue.ItemClassOrMissing?.CanStack() ?? false))
                return;
            XUiC_ItemStack slot = SlotUnderCursor(ui, cursor);
            if (slot == null || !Accepts(slot, held, dnd))
                return;
            active = true;
            button = left ? UICamera.MouseButton.LeftButton : UICamera.MouseButton.RightButton;
            visited.Clear();
            rejected.Clear();
            Add(slot);
        }

        private static void Add(XUiC_ItemStack slot)
        {
            visited.Add(slot);
            // Pomaranczowa ramka (IsHolding) jako podglad. Na pasku IsHolding oznacza aktywny slot - tam bez podgladu.
            if (slot.StackLocation == XUiC_ItemStack.StackLocationTypes.ToolBelt || slot.IsHolding)
                return;
            holdingBefore[slot] = false;
            slot.IsHolding = true;
        }

        private static void End()
        {
            foreach (KeyValuePair<XUiC_ItemStack, bool> kv in holdingBefore)
            {
                if (kv.Key != null && kv.Key.IsHolding)
                    kv.Key.IsHolding = kv.Value;
            }
            holdingBefore.Clear();
            visited.Clear();
            rejected.Clear();
            active = false;
        }

        // Odpowiednik UICamera.Raycast (NGUI.dll): wszystkie trafione collidery UI, tylko widoczne w swoich
        // panelach (przewijanie/przycinanie), wygrywa najwyzsza glebokosc. Jesli na wierzchu jest inne okno
        // niz slot, wynik to null (slot zasloniety).
        private static XUiC_ItemStack SlotUnderCursor(LocalPlayerUI ui, CursorControllerAbs cursor)
        {
            Camera cam = ui.camera;
            UICamera uiCam = ui.uiCamera;
            if (cam == null || uiCam == null)
                return null;
            Ray ray = cam.ScreenPointToRay(cursor.GetScreenPosition());
            int mask = cam.cullingMask & (int)uiCam.eventReceiverMask;
            RaycastHit[] hits = Physics.RaycastAll(ray, cam.farClipPlane - cam.nearClipPlane, mask, QueryTriggerInteraction.Collide);
            GameObject top = null;
            int topDepth = int.MinValue;
            for (int i = 0; i < hits.Length; i++)
            {
                GameObject go = hits[i].collider.gameObject;
                if (!VisibleInPanels(hits[i].point, go))
                    continue;
                int depth = NGUITools.CalculateRaycastDepth(go);
                if (depth > topDepth)
                {
                    topDepth = depth;
                    top = go;
                }
            }
            if (top == null)
                return null;
            List<XUiC_ItemStackGrid> grids = ui.activeItemStackGrids;
            for (int g = 0; g < grids.Count; g++)
            {
                XUiC_ItemStack[] slots = grids[g]?.GetItemStackControllers();
                if (slots == null)
                    continue;
                for (int i = 0; i < slots.Length; i++)
                {
                    XUiView v = slots[i]?.ViewComponent;
                    if (v?.collider != null && v.collider.gameObject == top && v.IsActiveInHierarchy)
                        return slots[i];
                }
            }
            return null;
        }

        private static bool VisibleInPanels(Vector3 worldPoint, GameObject go)
        {
            for (UIPanel p = NGUITools.FindInParents<UIPanel>(go); p != null; p = p.parentPanel)
            {
                if (!p.IsVisible(worldPoint))
                    return false;
            }
            return true;
        }

        private static int MaxFor(XUiC_ItemStack slot, ItemStack held)
        {
            int max = held.itemValue.ItemClassOrMissing.MaxCount;
            return slot.OverrideStackCount == -1 ? max : Mathf.Min(max, slot.OverrideStackCount);
        }

        // Te same warunki co SwapItem/HandleStackSwap w vanilla (XUiC_ItemStack.cs:1094-1326) + wykluczenia slotow specjalnych.
        private static bool Accepts(XUiC_ItemStack slot, ItemStack held, XUiC_DragAndDropWindow dnd)
        {
            if (slot == dnd.ItemStackControl || slot.IsDragAndDrop || slot.IsLocked || slot.StackLock || slot.SimpleClick || !slot.AllowDropping)
                return false;
            if (slot is XUiC_Creative2Stack)
                return false;
            switch (slot.StackLocation)
            {
                case XUiC_ItemStack.StackLocationTypes.Creative:
                case XUiC_ItemStack.StackLocationTypes.Equipment:
                case XUiC_ItemStack.StackLocationTypes.Cosmetics:
                case XUiC_ItemStack.StackLocationTypes.Part:
                case XUiC_ItemStack.StackLocationTypes.Merge:
                    return false;
            }
            if (slot.GetParentByType<XUiC_WorkstationOutputGrid>() != null)
                return false;
            if (!AcceptsWorkstationSlot(slot, held))
                return false;

            ItemStack cur = slot.ItemStack;
            if (cur.IsEmpty())
            {
                // XUiC_RequiredItemStack.CanSwap wywoluje FailedSwap (komunikat) - sprawdzamy to samo bez efektow ubocznych.
                bool allowed = slot is XUiC_RequiredItemStack req ? !req.TakeOnly && req.ItemAllowed(held) : slot.CanSwap(held);
                if (!allowed || !held.CanMoveTo(slot.StackLocation, slot.SlotNumber))
                    return false;
                if (slot.StackLocation == XUiC_ItemStack.StackLocationTypes.LootContainer
                    && dnd.PickUpType != XUiC_ItemStack.StackLocationTypes.LootContainer
                    && !held.itemValue.ItemClassOrMissing.CanPlaceInContainer())
                    return false;
                return true;
            }
            // Dokladanie do istniejacego stosu: jak HandleStackSwap (ten sam typ, bez jakosci, jest miejsce).
            return cur.itemValue.type == held.itemValue.type
                && !cur.itemValue.HasQuality
                && cur.count < MaxFor(slot, held);
        }

        // Stacje robocze: te same filtry co vanilla Shift-klik (XUiC_ItemStack.cs:1459, 1479), bo zwykly klik przyjmuje cokolwiek.
        //  - przetapianie kuzni (XUiC_WorkstationMaterialInputGrid): tablica TileEntity.Input = najpierw sloty przetapiania,
        //    potem ukryte stosy materialow (unit_*). Liczbe slotow bierzemy z TileEntity, wiec mody z wieksza liczba slotow dzialaja.
        //  - inne wejscia: tylko akceptowany material; paliwo: tylko przedmioty z wartoscia paliwa.
        private static bool AcceptsWorkstationSlot(XUiC_ItemStack slot, ItemStack held)
        {
            ItemClass itemClass = held.itemValue.ItemClassOrMissing;
            XUiC_WorkstationMaterialInputGrid smelt = slot.GetParentByType<XUiC_WorkstationMaterialInputGrid>();
            if (smelt != null)
            {
                TileEntityWorkstation te = smelt.WorkstationData?.TileEntity;
                if (te?.Input == null || te.MaterialNames == null)
                    return false;
                int smeltSlots = te.Input.Length - te.MaterialNames.Length;
                return slot.SlotNumber >= 0 && slot.SlotNumber < smeltSlots && itemClass != null && smelt.AcceptsMaterial(itemClass.MadeOfMaterial);
            }
            XUiC_WorkstationInputGrid input = slot.GetParentByType<XUiC_WorkstationInputGrid>();
            if (input != null)
                return itemClass != null && input.AcceptsMaterial(itemClass.MadeOfMaterial);
            if (slot.GetParentByType<XUiC_WorkstationFuelGrid>() != null)
                return itemClass?.FuelValue != null && itemClass.FuelValue.Value > 0;
            return true;
        }

        private static void Distribute(XUiC_DragAndDropWindow dnd)
        {
            dnd.xui.PopupMenuWindow?.Close();
            ItemStack held = dnd.CurrentStack;
            int n = visited.Count;
            int remaining = held.count;
            int per = button == UICamera.MouseButton.RightButton ? 1 : held.count / n;
            bool oneEach = per == 0; // mniej sztuk niz slotow: po 1 do pierwszych slotow
            if (oneEach)
                per = 1;

            XUiC_ItemStack firstChanged = null;
            for (int i = 0; i < n && remaining > 0; i++)
            {
                XUiC_ItemStack slot = visited[i];
                if (slot == null || !Accepts(slot, held, dnd))
                    continue;
                ItemStack cur = slot.ItemStack;
                int space = MaxFor(slot, held) - (cur.IsEmpty() ? 0 : cur.count);
                int give = Mathf.Min(per, Mathf.Min(space, remaining));
                if (give <= 0)
                    continue;
                ItemStack next;
                if (cur.IsEmpty())
                {
                    next = held.Clone();
                    next.count = give;
                }
                else
                {
                    next = cur.Clone();
                    next.count += give;
                }
                slot.ItemStack = next; // setter wywoluje SlotChangedEvent -> zapis do plecaka/skrzyni/stacji (i sync MP)
                remaining -= give;
                if (firstChanged == null)
                    firstChanged = slot;
            }

            if (firstChanged == null)
                return;
            ItemStack rest = ItemStack.Empty;
            if (remaining > 0)
            {
                rest = held.Clone();
                rest.count = remaining;
            }
            dnd.SetCurrentStack(rest, playPickupSound: false);
            firstChanged.PlayPlaceSound();
        }
    }

    [HarmonyPatch(typeof(XUiC_DragAndDropWindow), nameof(XUiC_DragAndDropWindow.Update))]
    public static class DragAndDropWindowUpdatePatch
    {
        public static void Postfix(XUiC_DragAndDropWindow __instance) => DragSplit.Tick(__instance);
    }

    [HarmonyPatch(typeof(XUiC_DragAndDropWindow), nameof(XUiC_DragAndDropWindow.OnClose))]
    public static class DragAndDropWindowClosePatch
    {
        public static void Prefix() => DragSplit.Reset();
    }

    // Podczas przeciagania (>= 2 sloty) slot, na ktorym wcisnieto przycisk, nie moze na puszczeniu
    // zrobic zwyklego odlozenia (HandleStackSwap) ani "upusc jeden" (HandleDropOne).
    [HarmonyPatch(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.Update))]
    public static class ItemStackUpdatePatch
    {
        public static void Prefix(XUiC_ItemStack __instance)
        {
            if (DragSplit.SuppressVanilla)
                __instance.lastClicked = false;
        }
    }

    // To samo dla upuszczania na tlo siatki (handleItemDrop dziala tylko z lastClicked siatki).
    [HarmonyPatch(typeof(XUiC_ItemStackGrid), "handleItemDrop")]
    public static class ItemStackGridDropPatch
    {
        public static void Prefix(XUiC_ItemStackGrid __instance)
        {
            if (DragSplit.SuppressVanilla)
                __instance.lastClicked = false;
        }
    }
}
