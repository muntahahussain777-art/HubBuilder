import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/vip_alloc_suggest.dart';
import '../logic/application_form_composer.dart';
import '../data/png_pdf.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same fields + compose preview as WinForms Application Form / Blank overlay.
/// VIP name search → auto plot/size/block/father/mobile/cnic/price (seats/fonts unchanged).
class ApplicationFormScreen extends StatefulWidget {
  const ApplicationFormScreen({
    super.key,
    required this.blankOverlay,
    this.onBack,
  });

  final bool blankOverlay;
  final VoidCallback? onBack;

  @override
  State<ApplicationFormScreen> createState() => _ApplicationFormScreenState();
}

class _ApplicationFormScreenState extends State<ApplicationFormScreen> {
  // --- Front ---
  late final TextEditingController _plotNo;
  late final TextEditingController _size;
  late final TextEditingController _commercial;
  late final TextEditingController _residentialPlot;
  late final TextEditingController _name;
  late final TextEditingController _father;
  late final TextEditingController _postal;
  late final TextEditingController _residential;
  late final TextEditingController _phoneOffice;
  late final TextEditingController _phoneHome;
  late final TextEditingController _careOf;
  late final TextEditingController _mobile;
  late final TextEditingController _overseas;
  late final TextEditingController _nationality;
  late final TextEditingController _nic;
  late final TextEditingController _heir;
  late final TextEditingController _place;
  late final TextEditingController _date;
  // --- Back ---
  late final TextEditingController _day;
  late final TextEditingController _month;
  late final TextEditingController _year;
  late final TextEditingController _acceptDate;
  late final TextEditingController _booker;
  late final TextEditingController _plotPrice;
  late final TextEditingController _corner;
  late final TextEditingController _roadside;
  late final TextEditingController _otherCharges;
  late final TextEditingController _grandTotal;

  bool _busy = false;
  bool _filling = false;
  List<VipAllocSuggest> _allocCache = const [];
  List<VipAllocSuggest> _nameHits = const [];
  bool _showNameHits = false;

  List<TextEditingController> get _all => [
        _plotNo,
        _size,
        _commercial,
        _residentialPlot,
        _name,
        _father,
        _postal,
        _residential,
        _phoneOffice,
        _phoneHome,
        _careOf,
        _mobile,
        _overseas,
        _nationality,
        _nic,
        _heir,
        _place,
        _date,
        _day,
        _month,
        _year,
        _acceptDate,
        _booker,
        _plotPrice,
        _corner,
        _roadside,
        _otherCharges,
        _grandTotal,
      ];

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _plotNo = TextEditingController();
    _size = TextEditingController();
    _commercial = TextEditingController();
    _residentialPlot = TextEditingController();
    _name = TextEditingController();
    _father = TextEditingController();
    _postal = TextEditingController();
    _residential = TextEditingController();
    _phoneOffice = TextEditingController();
    _phoneHome = TextEditingController();
    _careOf = TextEditingController();
    _mobile = TextEditingController();
    _overseas = TextEditingController();
    _nationality = TextEditingController(text: 'Pakistani');
    _nic = TextEditingController();
    _heir = TextEditingController();
    _place = TextEditingController();
    _date = TextEditingController(text: DateFormat('dd-MM-yyyy').format(now));
    _day = TextEditingController(text: '${now.day}');
    _month = TextEditingController(text: DateFormat('MMMM').format(now));
    _year = TextEditingController(text: '${now.year}');
    _acceptDate =
        TextEditingController(text: DateFormat('dd-MM-yyyy').format(now));
    _booker = TextEditingController();
    _plotPrice = TextEditingController();
    _corner = TextEditingController();
    _roadside = TextEditingController();
    _otherCharges = TextEditingController();
    _grandTotal = TextEditingController();
    _name.addListener(_onNameTyped);
    _loadCache();
  }

  Future<void> _loadCache() async {
    try {
      final cache = await VipAllocSuggestStore.loadActive();
      if (!mounted) return;
      setState(() => _allocCache = cache);
    } catch (_) {}
  }

  @override
  void dispose() {
    _name.removeListener(_onNameTyped);
    for (final c in _all) {
      c.dispose();
    }
    super.dispose();
  }

  void _onNameTyped() {
    if (_filling) return;
    final q = _name.text.trim();
    final hits = q.isEmpty
        ? const <VipAllocSuggest>[]
        : VipAllocSuggestStore.filter(_allocCache, q);
    setState(() {
      _nameHits = hits;
      _showNameHits = hits.isNotEmpty;
    });
  }

  void _applySuggest(VipAllocSuggest a) {
    _filling = true;
    _name.text = a.name;
    _father.text = a.father;
    _plotNo.text = a.plotNo;
    _size.text = a.size;
    _mobile.text = a.mobile;
    _nic.text = a.cnic;
    if (a.block.trim().isNotEmpty) _place.text = a.block;
    if (a.price > 0) {
      final p = VipAllocSuggestStore.formatMoney(a.price);
      _plotPrice.text = p;
      _grandTotal.text = p;
    }
    if (_nationality.text.trim().isEmpty) {
      _nationality.text = 'Pakistani';
    }
    _filling = false;
    setState(() {
      _nameHits = const [];
      _showNameHits = false;
    });
  }

  void _clear() {
    _filling = true;
    for (final c in _all) {
      c.clear();
    }
    final now = DateTime.now();
    _date.text = DateFormat('dd-MM-yyyy').format(now);
    _acceptDate.text = DateFormat('dd-MM-yyyy').format(now);
    _day.text = '${now.day}';
    _month.text = DateFormat('MMMM').format(now);
    _year.text = '${now.year}';
    _nationality.text = 'Pakistani';
    _filling = false;
    setState(() {
      _nameHits = const [];
      _showNameHits = false;
    });
  }

  ApplicationFormData _data() => ApplicationFormData(
        plotNo: _plotNo.text,
        size: _size.text,
        commercialPlot: _commercial.text,
        residentialPlot: _residentialPlot.text,
        name: _name.text,
        father: _father.text,
        postal: _postal.text,
        residential: _residential.text,
        phoneOffice: _phoneOffice.text,
        phoneHome: _phoneHome.text,
        careOf: _careOf.text,
        mobile: _mobile.text,
        overseas: _overseas.text,
        nationality: _nationality.text,
        nic: _nic.text,
        heir: _heir.text,
        place: _place.text,
        date: _date.text,
        day: _day.text,
        month: _month.text,
        year: _year.text,
        acceptDate: _acceptDate.text,
        booker: _booker.text,
        plotPrice: _plotPrice.text,
        corner: _corner.text,
        roadside: _roadside.text,
        otherCharges: _otherCharges.text,
        grandTotal: _grandTotal.text,
      );

  Future<void> _savePdf() async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      final pages = await ApplicationFormComposer.compose(
        data: _data(),
        blankOverlay: widget.blankOverlay,
      );
      final f = await PngPdf.save(
        pages: [pages.front, pages.back],
        fileBase: widget.blankOverlay ? 'ApplicationFormBlank' : 'ApplicationForm',
      );
      if (!mounted) return;
      if (f == null) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('PDF saved:\n${f.path}')),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('PDF failed: $e')),
      );
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _print() async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      final pages = await ApplicationFormComposer.compose(
        data: _data(),
        blankOverlay: widget.blankOverlay,
      );
      await PngPdf.printPages(
        pages: [pages.front, pages.back],
        name: widget.blankOverlay ? 'ApplicationFormBlank' : 'ApplicationForm',
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Print failed: $e')),
      );
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _preview() async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      final pages = await ApplicationFormComposer.compose(
        data: _data(),
        blankOverlay: widget.blankOverlay,
      );
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => _ApplicationFormPreviewPage(
            title: widget.blankOverlay
                ? 'Blank Overlay Preview'
                : 'Application Form Preview',
            frontPng: pages.front,
            backPng: pages.back,
            blankOverlay: widget.blankOverlay,
            fileBase: widget.blankOverlay
                ? 'ApplicationFormBlank'
                : 'ApplicationForm',
          ),
        ),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Preview failed: $e')),
      );
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _field(TextEditingController c, String label) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: TextField(
        controller: c,
        decoration: InputDecoration(labelText: label),
      ),
    );
  }

  Widget _nameField() {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextField(
            controller: _name,
            decoration: const InputDecoration(
              labelText: 'Name / نام (type to search)',
            ),
          ),
          if (_showNameHits)
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
                itemCount: _nameHits.length,
                itemBuilder: (context, i) {
                  final a = _nameHits[i];
                  return ListTile(
                    dense: true,
                    title: Text(
                      a.display,
                      style: const TextStyle(color: Colors.white, fontSize: 13),
                    ),
                    onTap: () => _applySuggest(a),
                  );
                },
              ),
            ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        SectionHeader(
          title: widget.blankOverlay
              ? 'Fill on Blank Form'
              : 'Application Form',
          subtitle: widget.blankOverlay
              ? 'Text-only overlay · pre-printed stock · درخواست فارم'
              : 'درخواست فارم · template print preview',
          trailing: widget.onBack == null
              ? null
              : IconButton(
                  onPressed: widget.onBack,
                  icon: const Icon(Icons.arrow_back),
                ),
        ),
        const SizedBox(height: 12),
        SoftPanel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                '--- Front Page ---',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      color: AppColors.tealBright,
                    ),
              ),
              const SizedBox(height: 8),
              _field(_plotNo, 'Plot No / پلاٹ نمبر'),
              _field(_size, 'Size / سائز'),
              _field(_commercial, 'Commercial Plot / کمرشل پلاٹ'),
              _field(_residentialPlot, 'Residential Plot / رہائشی پلاٹ'),
              _nameField(),
              _field(_father, 'Father / Husband / والد / شوہر'),
              _field(_postal, 'Postal Address / پوسٹل ایڈریس'),
              _field(_residential, 'Residential Address / رہائشی پتہ'),
              _field(_phoneOffice, 'Office Phone / فون دفتر'),
              _field(_phoneHome, 'Residence Phone / رہائش'),
              _field(_careOf, 'Care Of / بمعرفت'),
              _field(_mobile, 'Mobile / موبائل'),
              _field(_overseas, 'Overseas Number / بیرونی ملک'),
              _field(_nationality, 'Nationality / قومیت'),
              _field(_nic, 'NIC / این آئی سی'),
              _field(_heir, 'Heir Name & Address / وارث'),
              _field(_place, 'Place / جگہ (block auto)'),
              _field(_date, 'Date / تاریخ'),
              const SizedBox(height: 8),
              Text(
                '--- Back Page ---',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      color: AppColors.tealBright,
                    ),
              ),
              const SizedBox(height: 8),
              _field(_day, 'Day / مورخہ'),
              _field(_month, 'Month / مہینہ'),
              _field(_year, 'Year / سال'),
              _field(_acceptDate, 'Accept Date / تاریخ'),
              _field(_booker, 'Booker Name / بک کرنے والے کا نام'),
              _field(_plotPrice, 'Plot Price / پلاٹ کی قیمت'),
              _field(_corner, 'Corner Plot / کارنر پلاٹ'),
              _field(_roadside, 'Roadside Plot / روڈ سائیڈ'),
              _field(_otherCharges, 'Other Charges / اضافی اخراجات'),
              _field(_grandTotal, 'Grand Total / گرانڈ ٹوٹل'),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _clear,
                      child: const Text('Clear'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _preview,
                      child: Text(
                        _busy
                            ? 'Composing…'
                            : (widget.blankOverlay
                                ? 'Preview Overlay'
                                : 'Print Preview'),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _savePdf,
                      child: const Text('Save PDF'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _print,
                      child: const Text('Print'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _ApplicationFormPreviewPage extends StatelessWidget {
  const _ApplicationFormPreviewPage({
    required this.title,
    required this.frontPng,
    required this.backPng,
    required this.blankOverlay,
    required this.fileBase,
  });

  final String title;
  final Uint8List frontPng;
  final Uint8List backPng;
  final bool blankOverlay;
  final String fileBase;

  Future<void> _save(BuildContext context) async {
    try {
      final f = await PngPdf.save(
        pages: [frontPng, backPng],
        fileBase: fileBase,
      );
      if (!context.mounted || f == null) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('PDF saved:\n${f.path}')),
      );
    } catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('PDF failed: $e')),
      );
    }
  }

  Future<void> _print(BuildContext context) async {
    try {
      await PngPdf.printPages(
        pages: [frontPng, backPng],
        name: fileBase,
      );
    } catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Print failed: $e')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: Text(title),
          actions: [
            TextButton(
              onPressed: () => _save(context),
              child: const Text('Save PDF'),
            ),
            TextButton(
              onPressed: () => _print(context),
              child: const Text('Print'),
            ),
          ],
        ),
        body: ListView(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
          children: [
            Text(
              blankOverlay
                  ? 'White page + field text only (same seats as WinForms blank).'
                  : 'Filled template (same seats as WinForms Application Form).',
              style: Theme.of(context).textTheme.bodyMedium,
            ),
            const SizedBox(height: 12),
            Text('Front Page', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            SoftPanel(
              padding: EdgeInsets.zero,
              child: InteractiveViewer(
                minScale: 0.5,
                maxScale: 4,
                child: Image.memory(frontPng, fit: BoxFit.contain),
              ),
            ),
            const SizedBox(height: 16),
            Text('Back Page', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            SoftPanel(
              padding: EdgeInsets.zero,
              child: InteractiveViewer(
                minScale: 0.5,
                maxScale: 4,
                child: Image.memory(backPng, fit: BoxFit.contain),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
