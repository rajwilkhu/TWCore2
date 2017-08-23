using System;
using System.Collections.Generic;
using TWCore.Security;
using TWCore.Services;
using TWCore.Data;
using TWCore.Data.SqlServer;
using System.Linq;
using System.Threading.Tasks;

namespace TWCore.Tests
{
    public class SqlServerTest : ContainerParameterService
    {
        public SqlServerTest() : base("sqlservertest", "SqlServer Test") { }
        protected override void OnHandler(ParameterHandlerInfo info)
        {
            Core.Log.Warning("Starting SQL Server Test");

            IDalCity dalCity = new DalCity();

            using (var tW = Watch.Create("Sync Test"))
            {
                for (var i = 0; i < 30; i++)
                {
                    using (var w = Watch.Create())
                    {
                        var values = dalCity.GetAll();
                        w.Tap("GetAll");
                        var arr = values.ToArray();
                        w.Tap($"Entity Binding: {arr.Length} items");
                    }
                }
            }


            Console.ReadLine();

            Task.Run(async () => {
                IDalCityAsync dalCityAsync = new DalCityAsync();

                using (var tW = Watch.Create("Async Test"))
                {
                    for (var i = 0; i < 30; i++)
                    {
                        using (var w = Watch.Create())
                        {
                            var values = await dalCityAsync.GetAll().ConfigureAwait(false);
                            w.Tap("GetAll");
                            var arr = values.ToArray();
                            w.Tap($"Entity Binding: {arr.Length} items");
                        }
                    }
                }
            });

            Console.ReadLine();
        }
    }
    public class MiddleDalSettings : EntityDalSettings
    {
        public MiddleDalSettings()
        {
            ConnectionString = "Data Source=10.10.1.50;Initial Catalog=MRFLY_MIDDLE;User Id=MRFLY_SVC;Password=mISTERfLY13;Pooling=True";
            QueryType = DataAccessType.StoredProcedure;
        }
    }
    public class MiddleDatabaseDal : EntityDal
    {
        protected override IDataAccess OnGetDataAccess(EntityDalSettings settings)
        {
            Core.Log.Warning("Creating DataAccess Object");
            return new SqlServerDataAccess(Settings.ConnectionString, settings.QueryType);
        }
        protected override EntityDalSettings OnGetSettings()
        {
            Core.Log.Warning("Creating Settings Object");
            return new MiddleDalSettings();
        }
    }
    public class MiddleDatabaseDalAsync : EntityDalAsync
    {
        protected override IDataAccessAsync OnGetDataAccess(EntityDalSettings settings)
        {
            Core.Log.Warning("Creating DataAccess Object");
            return new SqlServerDataAccess(Settings.ConnectionString, settings.QueryType);
        }
        protected override EntityDalSettings OnGetSettings()
        {
            Core.Log.Warning("Creating Settings Object");
            return new MiddleDalSettings();
        }
    }

    public class EntCity
    {
        public Guid CityId { get; set; }
        public string Name { get; set; }
        public string Iata { get; set; }
        public int NumericCode { get; set; }
        public string Icao { get; set; }
        public string UIC { get; set; }
        public string ISO31662 { get; set; }
        public decimal? Gmt { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
    public interface IDalCity
    {
        IEnumerable<EntCity> GetAll();
        IEnumerable<EntCity> GetAll(string cultureInfo);
        EntCity GetByCityId(Guid cityId);
        EntCity GetByCityId(Guid cityId, string cultureInfo);
        EntCity GetByIata(string iata);
        EntCity GetByIata(string iata, string cultureInfo);
        int Delete(Guid cityId);
        int Insert(EntCity entity);
        int Update(EntCity entity);
    }
    public class DalCity : MiddleDatabaseDal, IDalCity
    {
        public IEnumerable<EntCity> GetAll()
        {
            using (var data = GetDataAccess())
                return data.SelectElements<EntCity>("sp_GEO_CITIES_GetAll");
        }

        public IEnumerable<EntCity> GetAll(string cultureInfo)
        {
            using (var data = GetDataAccess())
                return data.SelectElements<EntCity>("sp_GEO_CITIES_GetAllCultureInfo", new { CultureInfo = cultureInfo });
        }
        public EntCity GetByCityId(Guid cityId)
        {
            using (var data = GetDataAccess())
                return data.SelectElement<EntCity>("sp_GEO_CITIES_GetByCityId", new { CityId = cityId });
        }

        public EntCity GetByCityId(Guid cityId, string cultureInfo)
        {
            using (var data = GetDataAccess())
                return data.SelectElement<EntCity>("sp_GEO_CITIES_GetByCityIdCultureInfo", new { CityId = cityId, CultureInfo = cultureInfo });
        }

        public EntCity GetByIata(string iata)
        {
            using (var data = GetDataAccess())
                return data.SelectElement<EntCity>("sp_GEO_CITIES_GetByIata", new { Iata = iata });
        }

        public EntCity GetByIata(string iata, string cultureInfo)
        {
            using (var data = GetDataAccess())
                return data.SelectElement<EntCity>("sp_GEO_CITIES_GetByIataCultureInfo", new { Iata = iata, CultureInfo = cultureInfo });
        }
        public int Delete(Guid cityId)
        {
            using (var data = GetDataAccess())
                return data.ExecuteNonQuery("sp_GEO_CITIES_Del", new { CityId = cityId });
        }

        public int Insert(EntCity entity)
        {
            using (var data = GetDataAccess())
                return data.ExecuteNonQuery("sp_GEO_CITIES_Ins", entity);
        }

        public int Update(EntCity entity)
        {
            using (var data = GetDataAccess())
                return data.ExecuteNonQuery("sp_GEO_CITIES_Upd", entity);
        }
    }

    public interface IDalCityAsync
    {
        Task<IEnumerable<EntCity>> GetAll();
        Task<IEnumerable<EntCity>> GetAll(string cultureInfo);
        Task<EntCity> GetByCityId(Guid cityId);
        Task<EntCity> GetByCityId(Guid cityId, string cultureInfo);
        Task<EntCity> GetByIata(string iata);
        Task<EntCity> GetByIata(string iata, string cultureInfo);
        Task<int> Delete(Guid cityId);
        Task<int> Insert(EntCity entity);
        Task<int> Update(EntCity entity);
    }
    public class DalCityAsync : MiddleDatabaseDalAsync, IDalCityAsync
    {
        public async Task<IEnumerable<EntCity>> GetAll()
        {
            using (var data = GetDataAccess())
                return await data.SelectElementsAsync<EntCity>("sp_GEO_CITIES_GetAll").ConfigureAwait(false);
        }

        public async Task<IEnumerable<EntCity>> GetAll(string cultureInfo)
        {
            using (var data = GetDataAccess())
                return await data.SelectElementsAsync<EntCity>("sp_GEO_CITIES_GetAllCultureInfo", new { CultureInfo = cultureInfo }).ConfigureAwait(false);
        }
        public async Task<EntCity> GetByCityId(Guid cityId)
        {
            using (var data = GetDataAccess())
                return await data.SelectElementAsync<EntCity>("sp_GEO_CITIES_GetByCityId", new { CityId = cityId }).ConfigureAwait(false);
        }

        public async Task<EntCity> GetByCityId(Guid cityId, string cultureInfo)
        {
            using (var data = GetDataAccess())
                return await data.SelectElementAsync<EntCity>("sp_GEO_CITIES_GetByCityIdCultureInfo", new { CityId = cityId, CultureInfo = cultureInfo }).ConfigureAwait(false);
        }

        public async Task<EntCity> GetByIata(string iata)
        {
            using (var data = GetDataAccess())
                return await data.SelectElementAsync<EntCity>("sp_GEO_CITIES_GetByIata", new { Iata = iata }).ConfigureAwait(false);
        }

        public async Task<EntCity> GetByIata(string iata, string cultureInfo)
        {
            using (var data = GetDataAccess())
                return await data.SelectElementAsync<EntCity>("sp_GEO_CITIES_GetByIataCultureInfo", new { Iata = iata, CultureInfo = cultureInfo }).ConfigureAwait(false);
        }
        public async Task<int> Delete(Guid cityId)
        {
            using (var data = GetDataAccess())
                return await data.ExecuteNonQueryAsync("sp_GEO_CITIES_Del", new { CityId = cityId }).ConfigureAwait(false);
        }

        public async Task<int> Insert(EntCity entity)
        {
            using (var data = GetDataAccess())
                return await data.ExecuteNonQueryAsync("sp_GEO_CITIES_Ins", entity).ConfigureAwait(false);
        }

        public async Task<int> Update(EntCity entity)
        {
            using (var data = GetDataAccess())
                return await data.ExecuteNonQueryAsync("sp_GEO_CITIES_Upd", entity).ConfigureAwait(false);
        }
    }

}