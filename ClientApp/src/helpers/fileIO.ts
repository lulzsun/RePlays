import * as cheerio from 'cheerio';
// https://medium.com/@stefanhyltoft/scraping-html-tables-with-nodejs-request-and-cheerio-e3c6334f661b
// currently, this is useless since I have decided to fetch files/directories using C# for better performance
// but, keeping this here just incase for future purposes...

export async function getDirectories(dir: string) {
  let result: { name: string, modified: Date }[] = [];

  await fetch(dir).then(async res => {
    const $ = cheerio.load(await res.text());
    const links = $('body > section > table > tbody > tr');
    $(links).each(function(index, element){
      // You gotta love typescript...
      let name = ($(element).find('td > a')[0].children[0] as unknown as Text).data;
      let size = ($(element).find('td')[1].children[0] && ($(element).find('td')[1].children[0] as unknown as Text).data);
      let modified = new Date(($(element).find('td')[2].children[0] as unknown as Text).data);

      if(size === undefined) result.push({name, modified})
    });
  })

  return result;
}

export async function getFiles(dir: string) {
  let result: { name: string, size: number, modified: Date }[] = [];

  await fetch(dir).then(async res => {
    const $ = cheerio.load(await res.text());
    const links = $('body > section > table > tbody > tr');
    $(links).each(function(index, element){
      // You gotta love typescript...
      let name = ($(element).find('td > a')[0].children[0] as unknown as Text).data;
      let size = parseInt($(element).find('td')[1].children[0] && ($(element).find('td')[1].children[0] as unknown as Text).data.replace(/,/g, ''));
      let modified = new Date(($(element).find('td')[2].children[0] as unknown as Text).data);

      if(size !== undefined) result.push({name, size, modified})
    });
  })

  return result;
}

// getDirectories('/Plays').then(dirs => {
//   console.log('Folders: ', dirs);
// });
// getFiles('/Plays/League of Legends').then(files => {
//   console.log('Files: ', files);
// });