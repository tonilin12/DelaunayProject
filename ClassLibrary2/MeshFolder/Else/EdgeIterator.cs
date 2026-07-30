


namespace ClassLibrary2.MeshFolder.Else
{
    public struct EdgeIterator
    {
        private readonly HalfEdge? _firstEdge;
        private HalfEdge? _currentEdge;
        private bool _isFirstStep;
        private readonly Func<HalfEdge, HalfEdge?> _nextFunc;

        private EdgeIterator(HalfEdge? startEdge, Func<HalfEdge, HalfEdge?> nextFunc)
        {
            _firstEdge = startEdge;
            _currentEdge = null;
            _isFirstStep = true;
            _nextFunc = nextFunc ?? throw new ArgumentNullException(nameof(nextFunc));
        }

        public HalfEdge? Current => _currentEdge;

        public bool MoveNext()
        {
            if (_firstEdge == null) return false;

            if (_isFirstStep)
            {
                _currentEdge = _firstEdge;
                _isFirstStep = false;
                return true;
            }

            _currentEdge = _nextFunc(_currentEdge!);
            return _currentEdge != null && _currentEdge != _firstEdge;
        }

        public void Reset()
        {
            _currentEdge = null;
            _isFirstStep = true;
        }



        /// <summary>
        /// Vertex-edge iterator (CCW around vertex)
        /// </summary>
        public static EdgeIterator AroundVertex(Vertex v)
        {
            HalfEdge? firstEdge = v.OutgoingHalfEdge;

            return new EdgeIterator(firstEdge, e => e.Twin?.Next);
        }

        /// <summary>
        /// Face-edge iterator (cycle around face)
        /// </summary>
        public static EdgeIterator AroundFace(Face f)
        {
            HalfEdge? firstEdge = f.Edge;

            return new EdgeIterator(firstEdge, e => e.Next);
        }
    }
}
