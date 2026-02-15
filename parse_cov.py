import xml.etree.ElementTree as ET

modules = [
    ('Assets', 'apps/api/Tests/GameGuild.Assets.UnitTests/TestResults/coverage.xml'),
    ('Identity.Auth', 'apps/api/Tests/GameGuild.Identity.Authentication.UnitTests/TestResults/coverage.xml'),
    ('SharedKernel', 'apps/api/Tests/GameGuild.SharedKernel.UnitTests/TestResults/coverage.xml'),
    ('Identity.Authz', 'apps/api/Tests/GameGuild.Identity.Authorization.UnitTests/TestResults/coverage.xml'),
]

for name, path in modules:
    try:
        tree = ET.parse(path)
        root = tree.getroot()
        classes = []
        for cls in root.iter('class'):
            lines = list(cls.iter('line'))
            total = len(lines)
            uncov = sum(1 for l in lines if l.get('hits') == '0')
            if uncov > 10:
                cname = cls.get('name', '?').split('/')[-1]
                classes.append((uncov, total, cname))
        classes.sort(reverse=True)
        print(f'\n=== {name} (top 12 uncovered classes) ===')
        for uncov, total, cname in classes[:12]:
            print(f'  {uncov:4d}/{total:4d} uncov  {cname}')
    except Exception as e:
        print(f'{name}: ERROR {e}')
