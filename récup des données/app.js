
const btn = document.querySelector('.btn');
const btn1 = document.querySelector('.btn1');
const btn2 = document.createElement('button');
const myInput = document.querySelector("input");
const base = "https://api.stackexchange.com";
let page = 1
const firstPathURL = "/2.3/questions?page="
const secondPathURL = "&order=desc&sort=activity&site=stackoverflow";
var array = []
btn.onclick = (e) =>{
    let pathURL = firstPathURL+page.toString()+secondPathURL
    const url = base+pathURL;
    page+=3
    fetch(url)
    .then(rep => rep.json())
    .then(data => {
        array = array.concat(data.items)
    })
}
btn1.onclick = (e) =>{
  function download(filename, text) {
    var element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(text));
    element.setAttribute('download', filename);

    element.style.display = 'none';
    document.body.appendChild(element);

    element.click();

    document.body.removeChild(element);
  }
  const saveData = (dataArray)=>{
    var finalJSON = {
        items: dataArray
    }
    const jsonData = JSON.stringify(finalJSON,null,2)
    download("test.json",jsonData);
  }
saveData(array)
}
      