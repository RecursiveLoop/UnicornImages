var apiEndpoint ='https://do2co0jjtj.execute-api.ap-southeast-1.amazonaws.com/Prod/api/';

$(".filterImage").on("click",(e)=>{
    $(e).attr("width",500 );
})

$("#img-input").on("change",(e)=>{

    if (e.target.files.length>0)
    {
    var fd = new FormData();    
fd.append( 'file', e.target.files[0] );
var date = new Date();
var ticks = date.getTime();
var key=ticks + '_'  + e.target.files[0].name;
var apiEndpointPlusKey=apiEndpoint + "S3Proxy/" + key;

$.ajax({
    url: apiEndpointPlusKey,
    data: fd,
    processData: false,
    contentType: false,
    type: 'PUT',
    success: function(data){
      alert('Successfully uploaded image!');
      $("#div_Images").empty();
      var strImageHtml='';
        for (var i=0;i<data.length;i++)
        {

            if (data[i].FilterUrls!=null)
            {
                for (var k=0;k<data[i].FilterUrls.length;k++)
                {
                    strImageHtml+='<img class="filterImage" width="150px" data-key="' + data[i].Key + '" src="' + data[i].FilterUrls[k] + '"/>';
                }
            }
        }
        $("#div_Images").html(strImageHtml);

    

    }
  });
   }

})
