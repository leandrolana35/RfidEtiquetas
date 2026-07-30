// Arrasta elementos com [data-drag-id] dentro do canvas de preview da etiqueta.
// Usa um único listener por canvas (delegação) para funcionar mesmo quando
// elementos são adicionados/removidos dinamicamente (ex: ligar/desligar barcode).
// O arraste em si é feito 100% em JS (sem round-trip a cada pixel); só ao
// soltar é que avisamos o Blazor da posição final, via OnElementMoved.
window.etiquetaPreview = (function () {
    const registry = new Map();

    function init(canvasEl, dotNetRef) {
        const state = { dragging: null, startX: 0, startY: 0, origLeft: 0, origTop: 0 };

        const onPointerDown = (e) => {
            const target = e.target.closest('[data-drag-id]');
            if (!target || !canvasEl.contains(target)) return;
            state.dragging = target;
            state.startX = e.clientX;
            state.startY = e.clientY;
            state.origLeft = parseFloat(target.style.left) || 0;
            state.origTop = parseFloat(target.style.top) || 0;
            target.classList.add('dragging');
            try { target.setPointerCapture(e.pointerId); } catch { }
            e.preventDefault();
        };

        const onPointerMove = (e) => {
            if (!state.dragging) return;
            const dx = e.clientX - state.startX;
            const dy = e.clientY - state.startY;
            const rect = canvasEl.getBoundingClientRect();
            const elRect = state.dragging.getBoundingClientRect();
            const maxLeft = Math.max(0, rect.width - elRect.width);
            const maxTop = Math.max(0, rect.height - elRect.height);
            const newLeft = Math.min(Math.max(0, state.origLeft + dx), maxLeft);
            const newTop = Math.min(Math.max(0, state.origTop + dy), maxTop);
            state.dragging.style.left = newLeft + 'px';
            state.dragging.style.top = newTop + 'px';
        };

        const onPointerUp = () => {
            if (!state.dragging) return;
            const dragId = state.dragging.getAttribute('data-drag-id');
            const left = parseFloat(state.dragging.style.left) || 0;
            const top = parseFloat(state.dragging.style.top) || 0;
            state.dragging.classList.remove('dragging');
            state.dragging = null;
            dotNetRef.invokeMethodAsync('OnElementMoved', dragId, left, top);
        };

        canvasEl.addEventListener('pointerdown', onPointerDown);
        canvasEl.addEventListener('pointermove', onPointerMove);
        window.addEventListener('pointerup', onPointerUp);

        registry.set(canvasEl, { onPointerDown, onPointerMove, onPointerUp });
    }

    function destroy(canvasEl) {
        const h = registry.get(canvasEl);
        if (!h) return;
        canvasEl.removeEventListener('pointerdown', h.onPointerDown);
        canvasEl.removeEventListener('pointermove', h.onPointerMove);
        window.removeEventListener('pointerup', h.onPointerUp);
        registry.delete(canvasEl);
    }

    return { init, destroy };
})();
