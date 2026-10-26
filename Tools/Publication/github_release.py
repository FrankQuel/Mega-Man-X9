"""Publish this academic project with the user's existing Git credential helper.

Credentials stay in memory and are never printed or written to project files.
"""
from pathlib import Path
import argparse,http.client,json,os,subprocess,sys,time,urllib.error,urllib.parse,urllib.request

ROOT=Path(__file__).resolve().parents[2]
STATE=Path(__file__).with_name('publication.json')

def credential():
    token=os.environ.get('GH_TOKEN') or os.environ.get('GITHUB_TOKEN')
    if not token:
        env=os.environ.copy();env.update(GIT_TERMINAL_PROMPT='0',GCM_INTERACTIVE='Never')
        answer=subprocess.run(['git','credential','fill'],input='protocol=https\nhost=github.com\n\n',text=True,capture_output=True,env=env,timeout=30)
        values=dict(line.split('=',1) for line in answer.stdout.splitlines() if '=' in line)
        token=values.get('password')
    if not token:raise RuntimeError('No authenticated GitHub credential is available')
    return token

def api(token,path,method='GET',data=None):
    headers={'Authorization':'Bearer '+token,'Accept':'application/vnd.github+json','User-Agent':'MegaManX9-Academic-Publication','X-GitHub-Api-Version':'2022-11-28'}
    payload=json.dumps(data,ensure_ascii=False).encode('utf-8') if data is not None else None
    if payload is not None:headers['Content-Type']='application/json'
    request=urllib.request.Request('https://api.github.com'+path,data=payload,headers=headers,method=method)
    with urllib.request.urlopen(request,timeout=60) as response:return json.load(response)

def write_state(state):
    STATE.write_text(json.dumps(state,ensure_ascii=False,indent=2),encoding='utf-8')

def ensure_repository(token,name):
    user=api(token,'/user');owner=user['login']
    try:repo=api(token,f'/repos/{owner}/{name}')
    except urllib.error.HTTPError as error:
        if error.code!=404:raise
        repo=api(token,'/user/repos','POST',{'name':name,'description':'Proyecto académico no comercial de acción y plataformas 2D en Unity con X y Zero. Código y recursos multimedia distribuidos por separado.','private':False,'auto_init':False})
    if repo['owner']['login'].lower()!=owner.lower():raise RuntimeError('Unexpected repository owner')
    if repo.get('size',0)>0 and not STATE.exists():raise RuntimeError('The target repository already contains unrelated data; review before publishing')
    state={'owner':owner,'repository':name,'html_url':repo['html_url'],'clone_url':repo['clone_url'],'repository_created_or_verified':True,
           'published':False,'account_name':user.get('name') or owner,'account_id':user['id']}
    if STATE.exists():state={**json.loads(STATE.read_text(encoding='utf-8')),**{k:v for k,v in state.items() if k!='published'}}
    write_state(state);return state

def upload(token,url,path):
    parsed=urllib.parse.urlsplit(url.split('{',1)[0]+'?'+urllib.parse.urlencode({'name':path.name}))
    conn=http.client.HTTPSConnection(parsed.hostname,timeout=180)
    conn.putrequest('POST',parsed.path+'?'+parsed.query)
    for key,value in {'Authorization':'Bearer '+token,'Accept':'application/vnd.github+json','User-Agent':'MegaManX9-Academic-Publication',
                      'Content-Type':'application/zip' if path.suffix=='.zip' else 'application/octet-stream','Content-Length':str(path.stat().st_size)}.items():conn.putheader(key,value)
    conn.endheaders();sent=0;last_progress=0;start=time.time()
    with path.open('rb') as source:
        while chunk:=source.read(1024*1024):
            conn.send(chunk);sent+=len(chunk)
            if sent-last_progress>=16*1024*1024:print(json.dumps({'upload':path.name,'sent_mib':round(sent/1024**2),'elapsed_s':round(time.time()-start)}),flush=True);last_progress=sent
    response=conn.getresponse();payload=response.read();conn.close()
    if response.status!=201:raise RuntimeError(f'Upload failed with HTTP {response.status}: '+payload.decode('utf-8','replace')[:300])
    asset=json.loads(payload);return {'id':asset['id'],'name':asset['name'],'size':asset['size'],'browser_download_url':asset['browser_download_url'],'digest':asset.get('digest')}

def release(token,state,tag,paths):
    repository=f"/repos/{state['owner']}/{state['repository']}"
    try:item=api(token,repository+'/releases/tags/'+urllib.parse.quote(tag))
    except urllib.error.HTTPError as error:
        if error.code!=404:raise
        item=api(token,repository+'/releases','POST',{'tag_name':tag,'target_commitish':'main','name':'Proyecto U1 · Mega Man X9',
            'body':'Proyecto académico no comercial. Descarga el código y restaura el paquete de recursos antes de abrir Unity 6000.6.4f1. El ZIP conserva las rutas Assets/Recursos y SourceArt junto con los archivos .meta. Consulta README y la guía de restauración. Los personajes y recursos de Capcom y terceros conservan sus derechos; este proyecto no está afiliado a Capcom.',
            'draft':True,'prerelease':False})
    existing={asset['name']:asset for asset in item.get('assets',[])};uploaded=[]
    for path in paths:
        if path.name in existing:
            asset=existing[path.name]
            if asset['size']!=path.stat().st_size:raise RuntimeError('Existing release asset differs: '+path.name)
            uploaded.append({k:asset[k] for k in ['id','name','size','browser_download_url']})
        else:uploaded.append(upload(token,item['upload_url'],path))
        state.update(release_url=item['html_url'],release_tag=tag,release_assets=uploaded);write_state(state)
    api(token,repository+'/releases/'+str(item['id']),'PATCH',{'draft':False})
    state.update(published=True);write_state(state)
    return state

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--ensure-repo',action='store_true');parser.add_argument('--name',default='Mega-Man-X9')
    parser.add_argument('--release');parser.add_argument('--asset',action='append',default=[]);args=parser.parse_args()
    token=credential()
    state=ensure_repository(token,args.name) if args.ensure_repo else json.loads(STATE.read_text(encoding='utf-8'))
    if args.release:state=release(token,state,args.release,[Path(p).resolve() for p in args.asset])
    print(json.dumps({k:v for k,v in state.items() if k not in {'account_id'}},ensure_ascii=False,indent=2))

if __name__=='__main__':main()
