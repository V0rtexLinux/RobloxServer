import re
p = "RobloxServer.Web/RobloxServer.Web.csproj"
raw = open(p, "rb").read()
bom = raw.startswith(b"\xef\xbb\xbf")
t = raw.decode("utf-8-sig")
nl = "\r\n" if "\r\n" in t else "\n"
text = open("csproj-linhas.txt", encoding="utf-8").read().replace("\r", "")
blocks = re.findall(r'    <Compile Include=.*?(?:/>|</Compile>)|    <Content Include=.*?/>', text, flags=re.S)
add = [b for b in blocks if b.split('Include="')[1].split('"')[0] not in t]
content = [b for b in add if b.lstrip().startswith("<Content")]
comp = [b for b in add if b.lstrip().startswith("<Compile")]
def j(l):
    return "".join(x.replace("\n", nl) + nl for x in l)
def after(pattern, s, ins):
    m = re.search(pattern + r'[^\n]*\n', s)
    assert m, "ancora nao encontrada: " + pattern
    return s[:m.end()] + ins + s[m.end():]
t = after(r'<Compile Include="Code[\\/]Data[\\/]Forum\.cs"\s*/>', t, j(comp))
t = after(r'<Content Include="Catalog\.aspx"\s*/>', t, j(content))
open(p, "wb").write((b"\xef\xbb\xbf" if bom else b"") + t.encode("utf-8"))
print("adicionadas", len(content), "Content e", len(comp), "Compile")
