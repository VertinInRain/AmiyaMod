# -*- coding: utf-8 -*-
"""
按 isHarryh/Ark-Models(models_data.json) 精准下载阿米娅相关 Spine 资源
(.skel 二进制骨骼 / .atlas 图集描述 / .png 纹理), 来源: 明日方舟游戏资源(版权属鹰角网络)。
输出到本文件夹: 每个条目一个子目录, 外加 manifest.csv 与 说明.txt
"""
import csv, json, os, time, urllib.parse, urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = 'isHarryh/Ark-Models'
BR = 'main'
UA = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36'}

def get(url, timeout=180):
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.read()

def dl(url, dest):
    if os.path.exists(dest) and os.path.getsize(dest) >= 100:
        return True
    for attempt in range(3):
        try:
            req = urllib.request.Request(url, headers=UA)
            with urllib.request.urlopen(req, timeout=240) as r, open(dest, 'wb') as f:
                f.write(r.read())
            if os.path.getsize(dest) < 100:
                os.remove(dest); raise IOError('too small')
            return True
        except Exception as e:
            print('  dl fail(%d) %s %s' % (attempt + 1, url[-70:], str(e)[:100]))
            time.sleep(2)
    return False

def main():
    os.makedirs(HERE, exist_ok=True)
    idx_url = 'https://raw.githubusercontent.com/%s/%s/models_data.json' % (REPO, BR)
    print('fetch index...')
    data = json.loads(get(idx_url))
    entries = data['data']
    storage = data.get('storageDirectory') or {}
    print('total entries:', len(entries))

    amiya = {k: v for k, v in entries.items() if 'amiya' in k.lower()}
    print('amiya entries:', len(amiya))

    rows = []
    ok_all = 0
    meta = {}
    for key, ent in amiya.items():
        folder = storage.get(ent.get('type'), 'models')
        al = ent.get('assetList') or {}
        files = {}
        # assetList 形如 {'.atlas': 'build_char_...atlas', '.png': ..., '.skel': ...}
        for ext, fname in al.items():
            files[ext.lstrip('.')] = fname
        # 兜底: 若无 assetList 用约定名
        if not files:
            for ext in ('skel', 'atlas', 'png'):
                files[ext] = 'build_%s.%s' % (ent.get('assetId') or key, ext)

        sub = os.path.join(HERE, key)
        os.makedirs(sub, exist_ok=True)
        ok = 0
        for ext, fname in files.items():
            url = 'https://raw.githubusercontent.com/%s/%s/%s/%s/%s' % (
                REPO, BR, urllib.parse.quote(folder), urllib.parse.quote(key), urllib.parse.quote(fname))
            dest = os.path.join(sub, fname)
            if dl(url, dest):
                ok += 1
                ok_all += 1
            else:
                print('  FAILED', key, fname)
            time.sleep(0.2)
        # 清理空目录
        if ok == 0:
            import shutil
            shutil.rmtree(sub, ignore_errors=True)
            print('  -> skip entry (0 files):', key)
            continue
        sz = sum(os.path.getsize(os.path.join(sub, f)) for f in os.listdir(sub) if os.path.isfile(os.path.join(sub, f)))
        rows.append({
            '条目': key,
            'type': ent.get('type', ''),
            'assetId': ent.get('assetId', ''),
            '形态/皮肤名(官方)': ent.get('name', ''),
            'appellation': ent.get('appellation', ''),
            'skinGroupId': ent.get('skinGroupId', ''),
            'skinGroupName': ent.get('skinGroupName', ''),
            'sortTags': ','.join(ent.get('sortTags', []) or []),
            '文件': ','.join(files.values()),
            '文件数': len(files),
            '成功数': ok,
            '总字节': sz,
            '源码仓库': 'https://github.com/%s/tree/%s/%s/%s' % (REPO, BR, urllib.parse.quote(folder), urllib.parse.quote(key)),
        })
        meta[key] = {'entry': key, 'type': ent.get('type'), 'assetId': ent.get('assetId'), 'name': ent.get('name'),
                     'skinGroupName': ent.get('skinGroupName'), 'files': files}
        print('entry %-32s type=%s files=%d ok=%d bytes=%d' % (key, ent.get('type'), len(files), ok, sz))

    with open(os.path.join(HERE, 'manifest.csv'), 'w', newline='', encoding='utf-8-sig') as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)

    with open(os.path.join(HERE, 'meta.json'), 'w', encoding='utf-8') as f:
        json.dump(meta, f, ensure_ascii=False, indent=1)

    with open(os.path.join(HERE, '说明.txt'), 'w', encoding='utf-8') as f:
        f.write('阿米娅 Spine 战斗小人资源\n')
        f.write('来源: isHarryh/Ark-Models (github.com/isHarryh/Ark-Models), models_data.json 索引, 原始版权属 Hypergryph/鹰角网络。\n')
        f.write('格式: .skel=Spine 3.8 二进制骨骼, .atlas=图集描述, .png=纹理贴图; 可在 Spine 编辑器/支持 Spine 的引擎中加载。\n')
        f.write('注意: 官方游戏资源, 仅供学习/非商用 mod 参考; 发布分发请自行确认合规(游戏许可/鹰角规则), 并保留署名来源。\n')
        f.write('PRTS 相关: 该站动态立绘/干员查看器从 https://static.prts.wiki/spine38/ 读取同源资源, 可在对应干员页预览。\n')
        f.write('=' * 100 + '\n')
        for r in rows:
            f.write('[%s] %s  assetId=%s\n' % (r['条目'], r['形态/皮肤名(官方)'] or '', r['assetId']))
            f.write('    skinGroup: %s | tags: %s | 文件: %s\n' % (r['skinGroupName'], r['sortTags'], r['文件']))
            f.write('    仓库: %s\n' % r['源码仓库'])
    print('DONE entries=%d files_ok=%d' % (len(amiya), ok_all))

if __name__ == '__main__':
    main()
