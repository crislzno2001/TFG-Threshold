using System;
using UnityEngine;
using ThresholdGame.Core.Commands;

namespace ThresholdGame.Application.Commands
{
    /// <summary>
    /// Invocador de comandos con circular buffer de capacidad fija (300 slots).
    ///
    /// Diseño zero-allocation:
    ///   - El array se aloja una sola vez en el constructor.
    ///   - Span&lt;T&gt; se usa en los métodos de lectura/escritura masiva para
    ///     evitar copias intermedias sin tocar el heap.
    ///   - GetReplaySnapshot() es la ÚNICA operación que aloca (crea un array nuevo
    ///     para serialización/replay), y está marcada explícitamente.
    ///
    /// Uso típico (MonoBehaviour):
    ///   _invoker = new CommandInvoker();
    ///   _invoker.Execute(new MovementCommand(...));
    ///   _invoker.TryUndo();
    /// </summary>
    public sealed class CommandInvoker
    {
        // ── Constantes ────────────────────────────────────────────────────────

        public const int BufferCapacity = 300;

        // ── Estado interno ────────────────────────────────────────────────────

        private readonly ICommand[] _buffer = new ICommand[BufferCapacity];

        /// <summary>Índice del próximo slot de escritura (avanza circular).</summary>
        private int _writeIndex;

        /// <summary>Número de comandos válidos almacenados (máx = BufferCapacity).</summary>
        private int _count;

        // ── Propiedades públicas ──────────────────────────────────────────────

        public int  Count    => _count;
        public bool CanUndo  => _count > 0;
        public bool IsFull   => _count == BufferCapacity;

        // ── Escritura ─────────────────────────────────────────────────────────

        /// <summary>
        /// Ejecuta el comando y lo empuja al buffer.
        /// Si el buffer está lleno, el comando más antiguo se sobreescribe (FIFO).
        /// </summary>
        public void Execute(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            command.Execute();

            _buffer[_writeIndex] = command;
            _writeIndex = (_writeIndex + 1) % BufferCapacity;

            if (_count < BufferCapacity)
                _count++;
        }

        // ── Undo ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Revierte el último comando ejecutado.
        /// </summary>
        /// <returns>true si había algo que deshacer, false si el buffer estaba vacío.</returns>
        public bool TryUndo()
        {
            if (_count == 0) return false;

            _writeIndex = (_writeIndex - 1 + BufferCapacity) % BufferCapacity;
            _buffer[_writeIndex].Undo();
            _buffer[_writeIndex] = null;   // libera la referencia para GC
            _count--;

            return true;
        }

        // ── Iteración zero-allocation ─────────────────────────────────────────

        /// <summary>
        /// Itera sobre el historial (del más antiguo al más reciente) sin alocar.
        /// Usa Span&lt;T&gt; internamente para acceso directo al array.
        /// </summary>
        public void IterateHistory(Action<ICommand> action)
        {
            if (action == null || _count == 0) return;

            int start = StartIndex();
            Span<ICommand> span = _buffer;   // Span sobre el array sin copia

            if (start + _count <= BufferCapacity)
            {
                // Segmento contiguo — un solo recorrido
                foreach (ref var cmd in span.Slice(start, _count))
                    action(cmd);
            }
            else
            {
                // Buffer wrapeado — dos segmentos
                int firstLen = BufferCapacity - start;

                foreach (ref var cmd in span.Slice(start, firstLen))
                    action(cmd);

                foreach (ref var cmd in span.Slice(0, _count - firstLen))
                    action(cmd);
            }
        }

        // ── Replay ────────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve un snapshot del historial ordenado (antiguo → reciente).
        /// ALOCA un array nuevo: usar solo para serialización / inicio de replay.
        /// </summary>
        public ICommand[] GetReplaySnapshot()
        {
            var snapshot = new ICommand[_count];
            if (_count == 0) return snapshot;

            int start    = StartIndex();
            Span<ICommand> src = _buffer;
            Span<ICommand> dst = snapshot;

            if (start + _count <= BufferCapacity)
            {
                src.Slice(start, _count).CopyTo(dst);
            }
            else
            {
                int firstLen = BufferCapacity - start;
                src.Slice(start, firstLen).CopyTo(dst);
                src.Slice(0, _count - firstLen).CopyTo(dst.Slice(firstLen));
            }

            return snapshot;
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Limpia el buffer sin alocar (usa Span.Clear sobre el array existente).
        /// </summary>
        public void Clear()
        {
            Span<ICommand> span = _buffer;
            span.Clear();           // pone todo a null, sin new

            _writeIndex = 0;
            _count      = 0;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Índice del comando más antiguo en el buffer circular.</summary>
        private int StartIndex() =>
            (_writeIndex - _count + BufferCapacity) % BufferCapacity;

#if UNITY_EDITOR
        public void DebugDump()
        {
            Debug.Log($"[CommandInvoker] count={_count}/{BufferCapacity}  writeHead={_writeIndex}");
            IterateHistory(cmd => Debug.Log($"  {cmd}"));
        }
#endif
    }
}
