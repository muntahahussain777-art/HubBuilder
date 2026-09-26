import 'package:flutter/material.dart';

/// Mobile stand-in for WinForms Ctrl+1 combo suggest — same field, type to pick.
class TypeSuggestField<T> extends StatefulWidget {
  const TypeSuggestField({
    super.key,
    required this.label,
    required this.items,
    required this.display,
    required this.onSelected,
    this.selected,
    this.filterExtra,
  });

  final String label;
  final List<T> items;
  final String Function(T) display;
  final ValueChanged<T> onSelected;
  final T? selected;
  final bool Function(T item, String query)? filterExtra;

  @override
  State<TypeSuggestField<T>> createState() => _TypeSuggestFieldState<T>();
}

class _TypeSuggestFieldState<T> extends State<TypeSuggestField<T>> {
  late final TextEditingController _c;
  bool _show = false;

  @override
  void initState() {
    super.initState();
    _c = TextEditingController(
      text: widget.selected == null ? '' : widget.display(widget.selected as T),
    );
  }

  @override
  void didUpdateWidget(covariant TypeSuggestField<T> oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.selected != oldWidget.selected) {
      final next =
          widget.selected == null ? '' : widget.display(widget.selected as T);
      if (_c.text != next) _c.text = next;
    }
  }

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  List<T> get _hits {
    final q = _c.text.trim().toLowerCase();
    if (q.isEmpty) return widget.items.take(40).toList();
    return widget.items.where((item) {
      final d = widget.display(item).toLowerCase();
      if (d.contains(q)) return true;
      return widget.filterExtra?.call(item, q) ?? false;
    }).take(40).toList();
  }

  @override
  Widget build(BuildContext context) {
    final hits = _hits;
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextField(
            controller: _c,
            decoration: InputDecoration(
              labelText: widget.label,
              suffixIcon: const Icon(Icons.search, size: 20),
            ),
            onChanged: (_) => setState(() => _show = true),
            onTap: () => setState(() => _show = true),
          ),
          if (_show && hits.isNotEmpty)
            Container(
              margin: const EdgeInsets.only(top: 4),
              constraints: const BoxConstraints(maxHeight: 180),
              decoration: BoxDecoration(
                color: const Color(0xFF25294A),
                border: Border.all(color: Colors.white24),
                borderRadius: BorderRadius.circular(8),
              ),
              child: ListView.builder(
                shrinkWrap: true,
                itemCount: hits.length,
                itemBuilder: (context, i) {
                  final item = hits[i];
                  return ListTile(
                    dense: true,
                    title: Text(
                      widget.display(item),
                      style: const TextStyle(color: Colors.white, fontSize: 13),
                    ),
                    onTap: () {
                      _c.text = widget.display(item);
                      setState(() => _show = false);
                      widget.onSelected(item);
                    },
                  );
                },
              ),
            ),
        ],
      ),
    );
  }
}
